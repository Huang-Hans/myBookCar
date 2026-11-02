using MongoDB.Driver;

namespace LMS.BuildingBlocks.MongoDB.Concurrency;

// 乐观并发：仅当存储的版本等于期望版本时才匹配更新，并在同一次原子写入中把版本加一。
public static class VersionedUpdate
{
    public const string VersionField = "version";
    private const string IdField = "_id";

    // 没有匹配到文档时返回 false。scopeFilter 携带授权范围，因此由调用方区分“不存在”“不可见”和“版本冲突”。
    public static async Task<bool> TryApplyAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle session,
        FilterDefinition<TDocument> scopeFilter,
        long expectedVersion,
        UpdateDefinition<TDocument> update,
        CancellationToken cancellationToken)
    {
        var filter = Builders<TDocument>.Filter.And(
            scopeFilter,
            Builders<TDocument>.Filter.Eq(VersionField, expectedVersion));
        var versionedUpdate = Builders<TDocument>.Update.Combine(update, Builders<TDocument>.Update.Inc(VersionField, 1L));

        var result = await collection.UpdateOneAsync(session, filter, versionedUpdate, cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    // 以 _id 和期望版本为条件整体替换文档。replacement 的版本必须已经是 expectedVersion + 1。
    // 没有匹配到文档（不存在或版本已变化）时返回 false。
    public static async Task<bool> TryReplaceAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle session,
        string id,
        long expectedVersion,
        TDocument replacement,
        CancellationToken cancellationToken)
    {
        var filter = Builders<TDocument>.Filter.And(
            Builders<TDocument>.Filter.Eq(IdField, id),
            Builders<TDocument>.Filter.Eq(VersionField, expectedVersion));

        var result = await collection.ReplaceOneAsync(session, filter, replacement, cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }
}
