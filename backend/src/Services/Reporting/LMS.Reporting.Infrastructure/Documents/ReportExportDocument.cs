using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Reporting.Domain.Exports;
using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Reporting.Infrastructure.Documents;

// report_exports 在 Reporting 库内只保存运营报表导出：exportKind 固定为 REPORT。
internal sealed class ReportExportDocument
{
    private const string ReportKind = "REPORT";

    [BsonId]
    public string ExportId { get; init; } = string.Empty;

    [BsonElement("exportKind")]
    public string ExportKind { get; init; } = ReportKind;

    [BsonElement("reportType")]
    public string ReportType { get; init; } = string.Empty;

    [BsonElement("format")]
    public string Format { get; init; } = string.Empty;

    [BsonElement("filter")]
    public ReportExportFilterDocument Filter { get; init; } = new();

    [BsonElement("requestedBy")]
    public string RequestedBy { get; init; } = string.Empty;

    // 授权范围快照：全物业为 true 且物业列表为空；否则列出唯一的物业。
    [BsonElement("isAllProperties")]
    public bool IsAllProperties { get; init; }

    [BsonElement("propertyScopes")]
    public List<string> PropertyScopes { get; init; } = [];

    [BsonElement("requestedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime RequestedAt { get; init; }

    [BsonElement("state")]
    public string State { get; init; } = string.Empty;

    [BsonElement("resultFileId")]
    [BsonIgnoreIfNull]
    public string? ResultFileId { get; init; }

    [BsonElement("failureMessage")]
    [BsonIgnoreIfNull]
    public string? FailureMessage { get; init; }

    [BsonElement("expiresAt")]
    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ExpiresAt { get; init; }

    // 设计扩展：RUNNING 作业的租约到期时间，过期后可被重新领取。
    [BsonElement("leaseExpiresAt")]
    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LeaseExpiresAt { get; init; }

    [BsonElement("version")]
    public long Version { get; init; }

    public static ReportExportDocument FromDomain(ReportExport export) => new()
    {
        ExportId = export.ExportId,
        ReportType = WireEnum.ToWire(export.ReportType),
        Format = WireEnum.ToWire(export.Format),
        Filter = ReportExportFilterDocument.FromDomain(export.Filter),
        RequestedBy = export.RequestedBy,
        IsAllProperties = export.Scope.IsAllProperties,
        PropertyScopes = export.Scope.IsAllProperties ? [] : [export.Scope.PropertyId!],
        RequestedAt = UtcDateTimes.ToStored(export.RequestedAt),
        State = WireEnum.ToWire(export.State),
        ResultFileId = export.ResultFileId,
        FailureMessage = export.FailureMessage,
        ExpiresAt = export.ExpiresAt is null ? null : UtcDateTimes.ToStored(export.ExpiresAt.Value),
        LeaseExpiresAt = export.LeaseExpiresAt is null ? null : UtcDateTimes.ToStored(export.LeaseExpiresAt.Value),
        Version = export.Version
    };

    public ReportExport ToDomain() => new(
        ExportId,
        StoredValues.ParseEnum<ReportType>(ReportType, "reportType"),
        StoredValues.ParseEnum<ExportFormat>(Format, "format"),
        Filter.ToDomain(),
        RequestedBy,
        IsAllProperties ? PropertyScope.AllProperties : PropertyScope.ForProperty(PropertyScopes.Single()),
        UtcDateTimes.FromStored(RequestedAt),
        StoredValues.ParseEnum<ExportState>(State, "state"),
        ResultFileId,
        FailureMessage,
        ExpiresAt is null ? null : UtcDateTimes.FromStored(ExpiresAt.Value),
        LeaseExpiresAt is null ? null : UtcDateTimes.FromStored(LeaseExpiresAt.Value),
        Version);
}
