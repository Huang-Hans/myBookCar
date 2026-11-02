using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.BuildingBlocks.MongoDB.Errors;
using LMS.Reporting.Application.Abstractions;
using MongoDB.Driver;

namespace LMS.Reporting.Infrastructure.Persistence;

internal static class MongoReplace
{
    // 以期望版本为条件整体替换。唯一索引冲突转换成 DuplicateRecordException，版本不匹配返回 false。
    public static async Task<bool> VersionedAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle session,
        string id,
        long expectedVersion,
        TDocument replacement,
        CancellationToken cancellationToken)
    {
        try
        {
            return await VersionedUpdate.TryReplaceAsync(collection, session, id, expectedVersion, replacement, cancellationToken);
        }
        catch (Exception exception) when (MongoDuplicateKey.IsDuplicateKey(exception))
        {
            throw new DuplicateRecordException(collection.CollectionNamespace.CollectionName, exception);
        }
    }
}
