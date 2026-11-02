using System.Text.Json.Nodes;

namespace LMS.ApiGateway.Documentation.Merging;

// 按服务生成 x-tagGroups：聚合视图的侧边栏先按服务分组，再按资源标签展开，避免全部标签平铺。
internal static class OpenApiTagGroupBuilder
{
    private const string PathsKey = "paths";
    private const string TagsKey = "tags";

    public static JsonArray Build(IReadOnlyList<SourceDocument> documents)
    {
        var groups = new JsonArray();
        foreach (var document in documents)
        {
            groups.Add(new JsonObject
            {
                ["name"] = document.Source.GroupName,
                ["tags"] = new JsonArray(TagsOf(document).Select(tag => (JsonNode)JsonValue.Create(tag)!).ToArray())
            });
        }

        return groups;
    }

    // 按首次出现的顺序收集该服务全部接口使用的标签。
    private static IEnumerable<string> TagsOf(SourceDocument document) =>
        OperationsOf(document)
            .Select(operation => operation[TagsKey] as JsonArray)
            .OfType<JsonArray>()
            .SelectMany(tags => tags.Select(tag => tag?.GetValue<string>()))
            .OfType<string>()
            .Distinct(StringComparer.Ordinal);

    private static IEnumerable<JsonObject> OperationsOf(SourceDocument document) =>
        (document.Root[PathsKey] as JsonObject)?
            .Select(path => path.Value)
            .OfType<JsonObject>()
            .SelectMany(pathItem => pathItem.Select(member => member.Value))
            .OfType<JsonObject>()
        ?? [];
}
