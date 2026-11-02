using LMS.BuildingBlocks.MongoDB.Errors;
using LMS.Platform.Application.Abstractions;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Persistence;

internal static class MongoInsert
{
    // 把重复键错误转换成 Application 能识别的 DuplicateRecordException。
    // 瞬态事务错误（写冲突等）不在此转换，交给事务执行器重试。
    public static async Task OneAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle session,
        TDocument document,
        CancellationToken cancellationToken)
    {
        try
        {
            await collection.InsertOneAsync(session, document, cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (MongoDuplicateKey.IsDuplicateKey(exception))
        {
            throw new DuplicateRecordException(collection.CollectionNamespace.CollectionName, exception);
        }
    }
}
