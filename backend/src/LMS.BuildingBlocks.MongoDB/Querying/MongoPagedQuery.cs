using LMS.BuildingBlocks.Core.Querying;
using MongoDB.Driver;

namespace LMS.BuildingBlocks.MongoDB.Querying;

public static class MongoPagedQuery
{
    // 排序按不区分大小写的规则进行，因此 "alice" 与 "Bob" 按字母顺序而非按字节顺序排列。
    private static readonly Collation SortCollation = new("en", strength: CollationStrength.Secondary);

    public static async Task<PageResult<TDocument>> FindPageAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        SortDefinition<TDocument> sort,
        ListQuery query,
        CancellationToken cancellationToken)
    {
        var total = await collection.CountDocumentsAsync(
            filter, new CountOptions { Collation = SortCollation }, cancellationToken);
        var items = await collection
            .Find(filter, new FindOptions { Collation = SortCollation })
            .Sort(sort)
            .Skip(query.Skip)
            .Limit(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<TDocument>(items, checked((int)total));
    }
}
