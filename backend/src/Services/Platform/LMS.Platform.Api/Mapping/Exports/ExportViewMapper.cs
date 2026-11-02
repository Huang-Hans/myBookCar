using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Api.Contracts.Exports;
using LMS.Platform.Application.Exports.Models;
using LMS.Platform.Domain.Exports;

namespace LMS.Platform.Api.Mapping.Exports;

internal static class ExportViewMapper
{
    private const string AuditLogKind = "AUDIT_LOG";
    private const string WorkbookFormat = "XLSX";

    public static ExportOperationView ToView(AuditExport export) => new(
        export.ExportId,
        AuditLogKind,
        null,
        WorkbookFormat,
        export.RequestedAt.UtcDateTime,
        WireEnum.ToWire(export.State),
        export.ResultFileId,
        export.FailureMessage,
        export.ExpiresAt?.UtcDateTime);

    public static DownloadDescriptionView ToView(ExportDownload download) =>
        new(download.Url, download.FileName, download.ContentType, download.ExpiresAt.UtcDateTime);
}
