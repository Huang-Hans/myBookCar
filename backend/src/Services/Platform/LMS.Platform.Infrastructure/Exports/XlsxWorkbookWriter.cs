using System.IO.Compression;
using System.Text;
using System.Xml;
using LMS.Platform.Application.Exports.Ports;

namespace LMS.Platform.Infrastructure.Exports;

// 流式写出最小可用的 xlsx（OpenXML）：一张工作表，所有单元格为内联文本，表头加粗。
// 行数据边读边写，不在内存里保留整张表；ZIP 条目写入顺序固定，Excel / LibreOffice / 常见解析库都能打开。
internal sealed class XlsxWorkbookWriter : IWorkbookWriter
{
    private const string SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const int MaxCellCharacters = 32767;
    private const int HeaderStyleIndex = 1;
    private const int MaxColumns = 16384;

    public async Task WriteAsync(
        Stream output,
        string sheetName,
        IReadOnlyList<string> headers,
        IAsyncEnumerable<IReadOnlyList<string>> rows,
        CancellationToken cancellationToken)
    {
        if (headers.Count is 0 or > MaxColumns)
        {
            throw new ArgumentException("The sheet must have between 1 and 16384 columns.", nameof(headers));
        }

        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8);
        await WriteEntryAsync(archive, "[Content_Types].xml", WriteContentTypesAsync, cancellationToken);
        await WriteEntryAsync(archive, "_rels/.rels", WriteRootRelationshipsAsync, cancellationToken);
        await WriteEntryAsync(archive, "xl/workbook.xml", (writer, _) => WriteWorkbookAsync(writer, sheetName), cancellationToken);
        await WriteEntryAsync(archive, "xl/_rels/workbook.xml.rels", WriteWorkbookRelationshipsAsync, cancellationToken);
        await WriteEntryAsync(archive, "xl/styles.xml", WriteStylesAsync, cancellationToken);
        await WriteEntryAsync(archive, "xl/worksheets/sheet1.xml", (writer, token) => WriteSheetAsync(writer, headers, rows, token), cancellationToken);
    }

    private static async Task WriteEntryAsync(
        ZipArchive archive,
        string path,
        Func<XmlWriter, CancellationToken, Task> write,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        await using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Async = true, Encoding = new UTF8Encoding(false) });
        await writer.WriteStartDocumentAsync(standalone: true);
        await write(writer, cancellationToken);
        await writer.WriteEndDocumentAsync();
    }

    private static async Task WriteContentTypesAsync(XmlWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, "Types", ContentTypesNamespace);
        await WriteContentTypeAsync(writer, "Default", "Extension", "rels", "application/vnd.openxmlformats-package.relationships+xml");
        await WriteContentTypeAsync(writer, "Default", "Extension", "xml", "application/xml");
        await WriteContentTypeAsync(writer, "Override", "PartName", "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
        await WriteContentTypeAsync(writer, "Override", "PartName", "/xl/worksheets/sheet1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
        await WriteContentTypeAsync(writer, "Override", "PartName", "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
        await writer.WriteEndElementAsync();
    }

    private static async Task WriteContentTypeAsync(XmlWriter writer, string element, string keyName, string key, string contentType)
    {
        await writer.WriteStartElementAsync(null, element, null);
        await writer.WriteAttributeStringAsync(null, keyName, null, key);
        await writer.WriteAttributeStringAsync(null, "ContentType", null, contentType);
        await writer.WriteEndElementAsync();
    }

    private static async Task WriteRootRelationshipsAsync(XmlWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, "Relationships", PackageRelationshipsNamespace);
        await WriteRelationshipAsync(writer, "rId1", "officeDocument", "xl/workbook.xml");
        await writer.WriteEndElementAsync();
    }

    private static async Task WriteWorkbookRelationshipsAsync(XmlWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, "Relationships", PackageRelationshipsNamespace);
        await WriteRelationshipAsync(writer, "rId1", "worksheet", "worksheets/sheet1.xml");
        await WriteRelationshipAsync(writer, "rId2", "styles", "styles.xml");
        await writer.WriteEndElementAsync();
    }

    private static async Task WriteRelationshipAsync(XmlWriter writer, string id, string type, string target)
    {
        await writer.WriteStartElementAsync(null, "Relationship", null);
        await writer.WriteAttributeStringAsync(null, "Id", null, id);
        await writer.WriteAttributeStringAsync(null, "Type", null, $"{RelationshipsNamespace}/{type}");
        await writer.WriteAttributeStringAsync(null, "Target", null, target);
        await writer.WriteEndElementAsync();
    }

    private static async Task WriteWorkbookAsync(XmlWriter writer, string sheetName)
    {
        await writer.WriteStartElementAsync(null, "workbook", SpreadsheetNamespace);
        await writer.WriteAttributeStringAsync("xmlns", "r", null, RelationshipsNamespace);
        await writer.WriteStartElementAsync(null, "sheets", null);
        await writer.WriteStartElementAsync(null, "sheet", null);
        await writer.WriteAttributeStringAsync(null, "name", null, sheetName);
        await writer.WriteAttributeStringAsync(null, "sheetId", null, "1");
        await writer.WriteAttributeStringAsync("r", "id", RelationshipsNamespace, "rId1");
        await writer.WriteEndElementAsync();
        await writer.WriteEndElementAsync();
        await writer.WriteEndElementAsync();
    }

    // 两种单元格样式：0 为默认，1 为加粗（表头）。
    private static async Task WriteStylesAsync(XmlWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, "styleSheet", SpreadsheetNamespace);
        await writer.WriteRawAsync("<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>");
        await writer.WriteRawAsync("<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>");
        await writer.WriteRawAsync("<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>");
        await writer.WriteRawAsync("<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>");
        await writer.WriteRawAsync("<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs>");
        await writer.WriteEndElementAsync();
    }

    private static async Task WriteSheetAsync(
        XmlWriter writer,
        IReadOnlyList<string> headers,
        IAsyncEnumerable<IReadOnlyList<string>> rows,
        CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, "worksheet", SpreadsheetNamespace);
        await writer.WriteStartElementAsync(null, "sheetData", null);
        await WriteRowAsync(writer, 1, headers, HeaderStyleIndex);

        var rowNumber = 1;
        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            await WriteRowAsync(writer, ++rowNumber, row, styleIndex: null);
        }

        await writer.WriteEndElementAsync();
        await writer.WriteEndElementAsync();
    }

    private static async Task WriteRowAsync(XmlWriter writer, int rowNumber, IReadOnlyList<string> cells, int? styleIndex)
    {
        await writer.WriteStartElementAsync(null, "row", null);
        await writer.WriteAttributeStringAsync(null, "r", null, rowNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
        for (var column = 0; column < cells.Count; column++)
        {
            await WriteCellAsync(writer, ColumnName(column) + rowNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), cells[column], styleIndex);
        }

        await writer.WriteEndElementAsync();
    }

    private static async Task WriteCellAsync(XmlWriter writer, string reference, string text, int? styleIndex)
    {
        await writer.WriteStartElementAsync(null, "c", null);
        await writer.WriteAttributeStringAsync(null, "r", null, reference);
        await writer.WriteAttributeStringAsync(null, "t", null, "inlineStr");
        if (styleIndex is not null)
        {
            await writer.WriteAttributeStringAsync(null, "s", null, styleIndex.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        await writer.WriteStartElementAsync(null, "is", null);
        await writer.WriteStartElementAsync(null, "t", null);
        await writer.WriteAttributeStringAsync("xml", "space", null, "preserve");
        await writer.WriteStringAsync(Sanitize(text));
        await writer.WriteEndElementAsync();
        await writer.WriteEndElementAsync();
        await writer.WriteEndElementAsync();
    }

    // 去掉 XML 不允许的字符并限制 Excel 单元格长度，避免个别脏数据让整个导出失败。
    private static string Sanitize(string text)
    {
        var builder = new StringBuilder(Math.Min(text.Length, MaxCellCharacters));
        for (var index = 0; index < text.Length && builder.Length < MaxCellCharacters; index++)
        {
            if (XmlConvert.IsXmlChar(text[index]))
            {
                builder.Append(text[index]);
            }
            else if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[index + 1], text[index]))
            {
                builder.Append(text[index]).Append(text[++index]);
            }
        }

        return builder.ToString();
    }

    private static string ColumnName(int zeroBasedIndex)
    {
        var name = new StringBuilder();
        for (var index = zeroBasedIndex + 1; index > 0; index = (index - 1) / 26)
        {
            name.Insert(0, (char)('A' + ((index - 1) % 26)));
        }

        return name.ToString();
    }
}
