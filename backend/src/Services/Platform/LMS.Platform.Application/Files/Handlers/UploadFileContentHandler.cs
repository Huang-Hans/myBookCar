using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Files.Commands;
using LMS.Platform.Application.Files.Models;
using LMS.Platform.Application.Files.Options;
using LMS.Platform.Application.Files.Ports;
using LMS.Platform.Application.Files.Services;
using LMS.Platform.Domain.Files;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Files.Handlers;

// 接收上传的二进制内容：校验签名与失效时间，流式写入，核对大小和 sha256。
// 扫描器尚未接入：核对通过即置为 CLEAN（同时标记已存储），接入后改由扫描结果决定。
internal sealed class UploadFileContentHandler(
    IFileMetadataRepository files,
    IFileContentStore store,
    UploadUrlSigner signer,
    IOptions<FileStorageOptions> options,
    TimeProvider timeProvider) : IUploadFileContentHandler
{
    public async Task<OperationResult<FileMetadata>> HandleAsync(UploadFileContentCommand command, CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {
            return OperationResult<FileMetadata>.Failed(OperationOutcome.SourceUnavailable);
        }

        if (!signer.IsValid(command.FileId, command.ExpiresAtUnixSeconds, command.Signature))
        {
            return OperationResult<FileMetadata>.Failed(OperationOutcome.Forbidden);
        }

        var file = await files.FindAsync(command.FileId, cancellationToken);
        var rejection = CheckUploadable(file, command);

        return rejection ?? await StoreAsync(file!, command, cancellationToken);
    }

    private static OperationResult<FileMetadata>? CheckUploadable(FileMetadata? file, UploadFileContentCommand command)
    {
        if (file is null)
        {
            return OperationResult<FileMetadata>.Failed(OperationOutcome.NotFound);
        }

        if (file.ContentStoredAt is not null || file.ScanStatus != ScanStatus.Quarantined)
        {
            return OperationResult<FileMetadata>.Failed(OperationOutcome.DuplicateConflict);
        }

        return MediaTypeMatches(file.ContentType, command.ContentType)
            ? null
            : OperationResult<FileMetadata>.Invalid([new FieldIssue("Content-Type", "Must match the registered content type")]);
    }

    private static bool MediaTypeMatches(string registered, string? header) =>
        header is not null && string.Equals(header.Split(';')[0].Trim(), registered, StringComparison.OrdinalIgnoreCase);

    private async Task<OperationResult<FileMetadata>> StoreAsync(
        FileMetadata file,
        UploadFileContentCommand command,
        CancellationToken cancellationToken)
    {
        var stored = await store.WriteAsync(file.StorageReference, command.Content, file.SizeBytes, cancellationToken);
        if (stored.Outcome == StoreOutcome.AlreadyExists)
        {
            return OperationResult<FileMetadata>.Failed(OperationOutcome.DuplicateConflict);
        }

        if (stored.Outcome == StoreOutcome.Exceeded || stored.Length != file.SizeBytes || stored.Sha256 != file.Sha256)
        {
            return await RejectMismatchAsync(file, cancellationToken);
        }

        return await CompleteAsync(file, cancellationToken);
    }

    private async Task<OperationResult<FileMetadata>> RejectMismatchAsync(FileMetadata file, CancellationToken cancellationToken)
    {
        await store.DeleteAsync(file.StorageReference, cancellationToken);
        await files.MarkFailedAsync(file.FileId, cancellationToken);

        return OperationResult<FileMetadata>.Invalid([new FieldIssue("body", "Content does not match the registered size or sha256")]);
    }

    private async Task<OperationResult<FileMetadata>> CompleteAsync(FileMetadata file, CancellationToken cancellationToken)
    {
        var storedAt = timeProvider.GetUtcNow();
        if (!await files.MarkStoredAsync(file.FileId, storedAt, cancellationToken))
        {
            return OperationResult<FileMetadata>.Failed(OperationOutcome.DuplicateConflict);
        }

        return OperationResult<FileMetadata>.Succeeded(file with { ContentStoredAt = storedAt });
    }
}
