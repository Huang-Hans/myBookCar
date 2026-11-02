using LMS.BuildingBlocks.MongoDB.Errors;
using LMS.Dispatch.Application.Abstractions;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Persistence;

internal static class MongoInsert
{
    public static async Task ManyAsync<TDocument>(IMongoCollection<TDocument> collection,
        IClientSessionHandle session, IReadOnlyList<TDocument> documents, CancellationToken cancellationToken)
    {
        if (documents.Count == 0) return;
        try
        {
            await collection.InsertManyAsync(session, documents, cancellationToken: cancellationToken);
        }
        catch (MongoBulkWriteException<TDocument> exception) when (
            collection.CollectionNamespace.CollectionName == DispatchCollections.DriverShifts
            && exception.WriteErrors.Any(error => error.Message.Contains("index: ux_driver_date ", StringComparison.Ordinal)))
        {
            throw new ShiftSchemaUpgradeRequiredException(exception);
        }
        catch (MongoBulkWriteException<TDocument> exception) when (
            exception.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey))
        {
            throw new DuplicateRecordException(collection.CollectionNamespace.CollectionName, exception);
        }
        catch (Exception exception) when (MongoDuplicateKey.IsDuplicateKey(exception))
        {
            throw new DuplicateRecordException(collection.CollectionNamespace.CollectionName, exception);
        }
    }

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
