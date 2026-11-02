using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Files.Ports;
using LMS.Reporting.Domain.Files;
using LMS.Reporting.Infrastructure.Documents;
using LMS.Reporting.Infrastructure.Persistence;
using LMS.Reporting.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Reporting.Infrastructure.Repositories;

internal sealed class MongoFileMetadataRepository(IMongoDatabase database) : IFileMetadataRepository
{
    private IMongoCollection<FileMetadataDocument> Collection =>
        database.GetCollection<FileMetadataDocument>(ReportingCollections.FileMetadata);

    public async Task<FileMetadata?> FindAsync(string fileId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.FileId == fileId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task InsertAsync(FileMetadata file, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), FileMetadataDocument.FromDomain(file), cancellationToken);
}
