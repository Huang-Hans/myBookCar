using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LMS.BuildingBlocks.MongoDB.Querying;

public static class MongoTextSearch
{
    // 在给定的存储字段上做不区分大小写的包含匹配。搜索文本按字面量处理，不当作正则表达式。
    public static FilterDefinition<TDocument> Contains<TDocument>(IEnumerable<string> fields, string text)
    {
        var pattern = new BsonRegularExpression(Regex.Escape(text), "i");

        return Builders<TDocument>.Filter.Or(fields.Select(field => Builders<TDocument>.Filter.Regex(field, pattern)));
    }
}
