using System.Text.Json.Nodes;

namespace LMS.ApiGateway.Documentation.Merging;

// 把文档内的 $ref 改指到重命名后的组件。
internal static class OpenApiReferenceRewriter
{
    private const string ReferenceKey = "$ref";

    public static void Rewrite(JsonNode? node, IReadOnlyDictionary<string, string> references)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                RewriteObject(jsonObject, references);
                break;
            case JsonArray jsonArray:
                RewriteItems(jsonArray, references);
                break;
        }
    }

    private static void RewriteObject(JsonObject jsonObject, IReadOnlyDictionary<string, string> references)
    {
        foreach (var (name, value) in jsonObject.ToList())
        {
            if (TryRename(name, value, references, out var renamed))
            {
                jsonObject[name] = renamed;
                continue;
            }

            Rewrite(value, references);
        }
    }

    private static void RewriteItems(JsonArray jsonArray, IReadOnlyDictionary<string, string> references)
    {
        foreach (var item in jsonArray)
        {
            Rewrite(item, references);
        }
    }

    private static bool TryRename(string name, JsonNode? value, IReadOnlyDictionary<string, string> references, out string renamed)
    {
        renamed = string.Empty;

        return name == ReferenceKey
            && value is JsonValue jsonValue
            && jsonValue.TryGetValue<string>(out var target)
            && references.TryGetValue(target, out renamed!);
    }
}
