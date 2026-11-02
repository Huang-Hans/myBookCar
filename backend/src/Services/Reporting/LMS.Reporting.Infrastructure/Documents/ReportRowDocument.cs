using LMS.BuildingBlocks.Core.Wire;
using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Reporting.Infrastructure.Documents;

// report_rows：一行一文档。payload 是按 rowType 判别的联合，读取时由 ReportPayloadReader 按类型逐字段映射，不接受任意额外形状。
internal sealed class ReportRowDocument
{
    [BsonId]
    public string RowId { get; init; } = string.Empty;

    [BsonElement("rowType")]
    public string RowType { get; init; } = string.Empty;

    [BsonElement("filter")]
    public ReportRowScopeDocument Filter { get; init; } = new();

    [BsonElement("payload")]
    public BsonDocument Payload { get; init; } = [];

    [BsonElement("sourceVersion")]
    public long SourceVersion { get; init; }

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; init; }

    public static ReportRowDocument From(ReportRow row) => new()
    {
        RowId = row.RowId,
        RowType = WireEnum.ToWire(row.RowType),
        Filter = ReportRowScopeDocument.Of(row.Scope),
        Payload = ReportPayloadWriter.Write(row.Payload),
        SourceVersion = row.SourceVersion,
        UpdatedAt = UtcDateTimes.ToStored(row.UpdatedAt)
    };

    public ReportRow ToDomain()
    {
        var type = StoredValues.ParseEnum<ReportType>(RowType, "rowType");

        return new ReportRow(RowId, type, Filter.ToDomain(), ReportPayloadReader.Read(type, Payload), SourceVersion, UtcDateTimes.FromStored(UpdatedAt));
    }
}
