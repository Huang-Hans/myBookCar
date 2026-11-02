using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Domain.Exports;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

// report_exports 在 Platform 库内只保存审计导出：exportKind 固定为 AUDIT_LOG，format 固定为 XLSX，reportType 为 null。
internal sealed class AuditExportDocument
{
    private const string AuditLogKind = "AUDIT_LOG";
    private const string WorkbookFormat = "XLSX";

    [BsonId]
    public string ExportId { get; init; } = string.Empty;

    [BsonElement("exportKind")]
    public string ExportKind { get; init; } = AuditLogKind;

    [BsonElement("format")]
    public string Format { get; init; } = WorkbookFormat;

    [BsonElement("filter")]
    public AuditExportFilterDocument Filter { get; init; } = new();

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

    public static AuditExportDocument FromDomain(AuditExport export) => new()
    {
        ExportId = export.ExportId,
        Filter = AuditExportFilterDocument.FromDomain(export.Filter),
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

    public AuditExport ToDomain() => new(
        ExportId,
        Filter.ToDomain(),
        RequestedBy,
        IsAllProperties ? new PropertyScope(true, null) : new PropertyScope(false, PropertyScopes.Single()),
        UtcDateTimes.FromStored(RequestedAt),
        StoredValues.ParseEnum<ExportState>(State, "state"),
        ResultFileId,
        FailureMessage,
        ExpiresAt is null ? null : UtcDateTimes.FromStored(ExpiresAt.Value),
        LeaseExpiresAt is null ? null : UtcDateTimes.FromStored(LeaseExpiresAt.Value),
        Version);
}
