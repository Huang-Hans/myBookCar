using LMS.BuildingBlocks.Core.Querying;
using MongoDB.Driver;

namespace LMS.BuildingBlocks.MongoDB.Querying;

public static class MongoSortFactory
{
    // fieldMap 把 API 排序字段映射到存储字段，只有映射中存在的字段才可能参与排序。
    public static SortDefinition<TDocument> Create<TDocument>(
        IReadOnlyList<SortTerm> terms,
        IReadOnlyDictionary<string, string> fieldMap)
    {
        var builder = Builders<TDocument>.Sort;
        var definitions = terms.Select(term => term.IsDescending
            ? builder.Descending(fieldMap[term.Field])
            : builder.Ascending(fieldMap[term.Field]));

        return builder.Combine(definitions);
    }
}
