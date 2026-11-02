using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Platform.Domain.Exports;

// 审计导出记录（report_exports，exportKind=AUDIT_LOG，format=XLSX）。Scope 是申请时的授权范围快照：
// 导出内容按它生成；读取和下载仍按调用者当前授权复核，范围缩小后不能继续获取。
public sealed record AuditExport(
    string ExportId,
    AuditExportFilter Filter,
    string RequestedBy,
    PropertyScope Scope,
    DateTimeOffset RequestedAt,
    ExportState State,
    string? ResultFileId,
    string? FailureMessage,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? LeaseExpiresAt,
    long Version)
{
    private const string IdPrefix = "export_";
    private const long FirstVersion = 1;

    public static AuditExport Create(AuditExportFilter filter, string requestedBy, PropertyScope scope, DateTimeOffset requestedAt) =>
        new(IdPrefix + Guid.CreateVersion7().ToString("N"), filter, requestedBy, scope, requestedAt, ExportState.Queued,
            null, null, null, null, FirstVersion);

    // 申请人当前范围必须覆盖导出快照范围，才能读取或下载该导出。
    public bool IsVisibleTo(PropertyScope caller) =>
        Scope.IsAllProperties ? caller.IsAllProperties : caller.Allows(Scope.PropertyId!);

    public bool IsExpiredAt(DateTimeOffset now) => ExpiresAt is not null && now >= ExpiresAt;
}
