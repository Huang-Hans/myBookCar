using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Reporting.Application.Exports.Models;
using LMS.Reporting.Application.Exports.Ports;
using LMS.Reporting.Application.Files.Options;
using LMS.Reporting.Application.Files.Ports;
using LMS.Reporting.Application.Files.Services;
using LMS.Reporting.Domain.Exports;
using Microsoft.Extensions.Options;

namespace LMS.Reporting.Application.Exports.Handlers;

internal sealed class ReportExportQueryHandler(
    IReportExportRepository exports,
    IFileMetadataRepository files,
    DownloadUrlSigner signer,
    IOptions<FileStorageOptions> storageOptions,
    TimeProvider timeProvider) : IReportExportQueryHandler
{
    public async Task<OperationResult<ReportExport>> GetAsync(string exportId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var export = await exports.FindAsync(exportId, cancellationToken);

        return export is not null && export.IsVisibleTo(scope)
            ? OperationResult<ReportExport>.Succeeded(export)
            : OperationResult<ReportExport>.Failed(OperationOutcome.NotFound);
    }

    public async Task<OperationResult<ExportDownload>> DescribeDownloadAsync(
        string exportId,
        PropertyScope scope,
        CancellationToken cancellationToken)
    {
        var export = await exports.FindAsync(exportId, cancellationToken);
        if (export is null || !export.IsVisibleTo(scope))
        {
            return OperationResult<ExportDownload>.Failed(OperationOutcome.NotFound);
        }

        var rejection = CheckDownloadable(export);
        if (rejection is not null)
        {
            return rejection;
        }

        return await DescribeAsync(export.ResultFileId!, cancellationToken);
    }

    // 未完成或结果已过期：请求本身合法但前置条件不满足，返回 422 / 42200（合同 EX03：未完成/过期 422）。
    private OperationResult<ExportDownload>? CheckDownloadable(ReportExport export)
    {
        if (export.State != ExportState.Succeeded || export.ResultFileId is null)
        {
            return OperationResult<ExportDownload>.Rejected([new FieldIssue("exportId", "The export result is not ready")]);
        }

        return export.IsExpiredAt(timeProvider.GetUtcNow())
            ? OperationResult<ExportDownload>.Rejected([new FieldIssue("exportId", "The export result has expired")])
            : null;
    }

    private async Task<OperationResult<ExportDownload>> DescribeAsync(string fileId, CancellationToken cancellationToken)
    {
        if (!storageOptions.Value.IsConfigured)
        {
            return OperationResult<ExportDownload>.Failed(OperationOutcome.SourceUnavailable);
        }

        var file = await files.FindAsync(fileId, cancellationToken);
        if (file is null)
        {
            return OperationResult<ExportDownload>.Failed(OperationOutcome.NotFound);
        }

        var (url, expiresAt) = signer.Create(file.FileId);

        return OperationResult<ExportDownload>.Succeeded(new ExportDownload(url, file.OriginalFileName, file.ContentType, expiresAt));
    }
}
