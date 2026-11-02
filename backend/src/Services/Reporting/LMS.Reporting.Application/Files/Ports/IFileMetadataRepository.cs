using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Domain.Files;

namespace LMS.Reporting.Application.Files.Ports;

public interface IFileMetadataRepository
{
    Task<FileMetadata?> FindAsync(string fileId, CancellationToken cancellationToken);

    Task InsertAsync(FileMetadata file, ITransactionContext context, CancellationToken cancellationToken);
}
