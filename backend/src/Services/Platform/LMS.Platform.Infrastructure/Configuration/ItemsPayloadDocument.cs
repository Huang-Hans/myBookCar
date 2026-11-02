using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration;

// 列表型配置区域的正文：一组强类型条目。
internal sealed class ItemsPayloadDocument<TItem>
{
    [BsonElement("items")]
    public List<TItem> Items { get; init; } = [];
}
