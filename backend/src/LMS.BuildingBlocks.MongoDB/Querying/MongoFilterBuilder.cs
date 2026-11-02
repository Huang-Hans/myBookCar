using MongoDB.Driver;

namespace LMS.BuildingBlocks.MongoDB.Querying;

// 条件拼接：只有条件成立时才追加筛选，避免在仓储里堆叠 if 分支。Build 以 AND 合并全部筛选。
public sealed class MongoFilterBuilder<TDocument>(FilterDefinition<TDocument> baseFilter)
{
    private readonly List<FilterDefinition<TDocument>> filters = [baseFilter];

    public MongoFilterBuilder<TDocument> When(bool condition, Func<FilterDefinition<TDocument>> filter)
    {
        if (condition)
        {
            filters.Add(filter());
        }

        return this;
    }

    public FilterDefinition<TDocument> Build() => Builders<TDocument>.Filter.And(filters);
}
