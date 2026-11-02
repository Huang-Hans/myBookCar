using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Files.Ports;
using LMS.Platform.Domain.Files;
using LMS.Platform.Infrastructure.Documents;
using LMS.Platform.Infrastructure.Persistence;
using LMS.Platform.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

internal sealed class MongoFileMetadataRepository(IMongoDatabase database) : IFileMetadataRepository
{
    private IMongoCollection<FileMetadataDocument> Collection =>
        database.GetCollection<FileMetadataDocument>(PlatformCollections.FileMetadata);

    public async Task<FileMetadata?> FindAsync(string fileId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.FileId == fileId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task InsertAsync(FileMetadata file, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), FileMetadataDocument.FromDomain(file), cancellationToken);

    // 内容已存储；扫描器尚未接入，这里暂时一并置为 CLEAN（接入后改由扫描结果决定）。
    public async Task<bool> MarkStoredAsync(string fileId, DateTimeOffset storedAt, CancellationToken cancellationToken)
    {
        var result = await Collection.UpdateOneAsync(
            NotYetStored(fileId),
            Builders<FileMetadataDocument>.Update
                .Set(candidate => candidate.ContentStoredAt, UtcDateTimes.ToStored(storedAt))
                .Set(candidate => candidate.ScanStatus, WireEnum.ToWire(ScanStatus.Clean)),
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    public async Task<bool> MarkFailedAsync(string fileId, CancellationToken cancellationToken)
    {
        var result = await Collection.UpdateOneAsync(
            NotYetStored(fileId),
            Builders<FileMetadataDocument>.Update.Set(candidate => candidate.ScanStatus, WireEnum.ToWire(ScanStatus.Failed)),
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    private static FilterDefinition<FileMetadataDocument> NotYetStored(string fileId) =>
        Builders<FileMetadataDocument>.Filter.And(
            Builders<FileMetadataDocument>.Filter.Eq(candidate => candidate.FileId, fileId),
            Builders<FileMetadataDocument>.Filter.Eq(candidate => candidate.ContentStoredAt, null));
}
