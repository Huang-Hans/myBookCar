using System.Globalization;
using LMS.Platform.Domain.Auditing;

namespace LMS.Platform.Application.Exports.Services;

// 审计导出的列与单元格文本：列与审计列表视图一致，时间为 UTC 的 ISO 8601。
internal static class AuditExportSheet
{
    public const string SheetName = "Audit log";

    private const string InstantFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public static IReadOnlyList<string> Headers { get; } =
    [
        "Audit ID", "Occurred at (UTC)", "Actor", "Operation", "Resource type", "Resource ID", "Property", "Outcome",
        "Trace ID", "Source service", "Change summary", "Reason reference", "Device", "Source IP", "Session reference"
    ];

    public static IReadOnlyList<string> RowOf(AuditRecord record) =>
    [
        record.AuditId,
        record.OccurredAt.UtcDateTime.ToString(InstantFormat, CultureInfo.InvariantCulture),
        record.Actor,
        record.Operation,
        record.ResourceType,
        record.ResourceId,
        record.PropertyId ?? string.Empty,
        record.Outcome.ToString(),
        record.TraceId,
        record.SourceService,
        record.ChangeSummary ?? string.Empty,
        record.ReasonReference ?? string.Empty,
        record.Device ?? string.Empty,
        record.SourceIp ?? string.Empty,
        record.SessionReference ?? string.Empty
    ];
}
