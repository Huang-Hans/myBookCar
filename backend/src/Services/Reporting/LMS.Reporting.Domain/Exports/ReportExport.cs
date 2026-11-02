using LMS.BuildingBlocks.Core.Scoping;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Domain.Exports;

// 运营报表导出记录（report_exports，exportKind=REPORT）。Scope 是申请时的授权范围快照；读取和下载按调用者当前授权复核。
public sealed record ReportExport(
    string ExportId,
    ReportType ReportType,
    ExportFormat Format,
    ReportExportFilter Filter,
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

    public static ReportExport Create(
        ReportType reportType,
        ExportFormat format,
        ReportExportFilter filter,
        string requestedBy,
        PropertyScope scope,
        DateTimeOffset requestedAt) =>
        new(IdPrefix + Guid.CreateVersion7().ToString("N"), reportType, format, filter, requestedBy, scope, requestedAt,
            ExportState.Queued, null, null, null, null, FirstVersion);

    public bool IsVisibleTo(PropertyScope caller) =>
        Scope.IsAllProperties ? caller.IsAllProperties : caller.Allows(Scope.PropertyId!);

    public bool IsExpiredAt(DateTimeOffset now) => ExpiresAt is not null && now >= ExpiresAt;
}
