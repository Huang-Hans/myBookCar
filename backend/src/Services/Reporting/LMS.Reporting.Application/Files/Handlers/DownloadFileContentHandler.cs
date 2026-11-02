using LMS.BuildingBlocks.Core.Operations;
using LMS.Reporting.Application.Exports.Ports;
using LMS.Reporting.Application.Files.Commands;
using LMS.Reporting.Application.Files.Models;
using LMS.Reporting.Application.Files.Options;
using LMS.Reporting.Application.Files.Ports;
using LMS.Reporting.Application.Files.Services;
using Microsoft.Extensions.Options;

namespace LMS.Reporting.Application.Files.Handlers;

// 提供导出结果文件的内容：只校验签名、文件归属导出且尚未过期；权限复核发生在签发下载描述时。
internal sealed class DownloadFileContentHandler(
    IFileMetadataRepository files,
    IReportExportRepository exports,
    IFileContentStore store,
    DownloadUrlSigner signer,
    IOptions<FileStorageOptions> options,
    TimeProvider timeProvider) : IDownloadFileContentHandler
{
    public async Task<OperationResult<FileDownload>> HandleAsync(DownloadFileContentCommand command, CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {
            return OperationResult<FileDownload>.Failed(OperationOutcome.SourceUnavailable);
        }

        if (!signer.IsValid(command.FileId, command.ExpiresAtUnixSeconds, command.Signature))
        {
            return OperationResult<FileDownload>.Failed(OperationOutcome.Forbidden);
        }

        var file = await files.FindAsync(command.FileId, cancellationToken);
        var export = file is null ? null : await exports.FindAsync(file.ExportId, cancellationToken);
        if (file is null || export is null || export.ResultFileId != file.FileId || export.IsExpiredAt(timeProvider.GetUtcNow()))
        {
            return OperationResult<FileDownload>.Failed(OperationOutcome.NotFound);
        }

        var content = store.OpenRead(file.StorageReference);

        return OperationResult<FileDownload>.Succeeded(new FileDownload(content, file.OriginalFileName, file.ContentType, file.SizeBytes));
    }
}
