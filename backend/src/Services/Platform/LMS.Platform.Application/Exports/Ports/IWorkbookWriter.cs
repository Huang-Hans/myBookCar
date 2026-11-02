namespace LMS.Platform.Application.Exports.Ports;

public interface IWorkbookWriter
{
    // 流式写出只有一张工作表的 xlsx：首行为表头，其余为数据行，所有单元格按文本写入。
    Task WriteAsync(
        Stream output,
        string sheetName,
        IReadOnlyList<string> headers,
        IAsyncEnumerable<IReadOnlyList<string>> rows,
        CancellationToken cancellationToken);
}
