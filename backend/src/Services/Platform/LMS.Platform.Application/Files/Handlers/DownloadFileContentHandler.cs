using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Exports.Ports;
using LMS.Platform.Application.Files.Commands;
using LMS.Platform.Application.Files.Models;
using LMS.Platform.Application.Files.Options;
using LMS.Platform.Application.Files.Ports;
using LMS.Platform.Application.Files.Services;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Files.Handlers;

// 提供导出结果文件的内容：只校验签名、文件归属导出且尚未过期；权限复核发生在签发下载描述时。
// 审批材料不经此入口下载。
internal sealed class DownloadFileContentHandler(
    IFileMetadataRepository files,
    IAuditExportRepository exports,
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
        if (file?.ExportId is null || file.ContentStoredAt is null)
        {
            return OperationResult<FileDownload>.Failed(OperationOutcome.NotFound);
        }

        var export = await exports.FindAsync(file.ExportId, cancellationToken);
        if (export is null || export.ResultFileId != file.FileId || export.IsExpiredAt(timeProvider.GetUtcNow()))
        {
            return OperationResult<FileDownload>.Failed(OperationOutcome.NotFound);
        }

        var content = store.OpenRead(file.StorageReference);

        return OperationResult<FileDownload>.Succeeded(new FileDownload(content, file.OriginalFileName, file.ContentType, file.SizeBytes));
    }
}
