using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Files;

namespace LMS.Platform.Application.Files.Ports;

public interface IFileMetadataRepository
{
    Task<FileMetadata?> FindAsync(string fileId, CancellationToken cancellationToken);

    Task InsertAsync(FileMetadata file, ITransactionContext context, CancellationToken cancellationToken);

    // 仅当内容尚未存储时标记已存储；已存储（并发上传）返回 false。
    // 扫描器尚未接入：已存储即暂时置为 CLEAN，接入后改由扫描结果决定。
    Task<bool> MarkStoredAsync(string fileId, DateTimeOffset storedAt, CancellationToken cancellationToken);

    // 内容与登记不符：仅当内容尚未存储时把状态置为 FAILED。
    Task<bool> MarkFailedAsync(string fileId, CancellationToken cancellationToken);
}
