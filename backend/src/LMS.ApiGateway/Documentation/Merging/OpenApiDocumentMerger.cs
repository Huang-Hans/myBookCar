using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LMS.ApiGateway.Documentation.Merging;

// 把多个下游服务的 OpenAPI 文档合并为一份：paths 取并集，components 同名冲突时加服务前缀，
// 标签按服务分组（x-tagGroups）。输入文档的顺序决定同名冲突时谁保留原名。
internal static class OpenApiDocumentMerger
{
    private const string PathsKey = "paths";
    private const string ComponentsKey = "components";
    private const string TagsKey = "tags";

    private static readonly JsonSerializerOptions OutputOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string Merge(IReadOnlyList<SourceDocument> documents, IReadOnlyList<string> unavailableServices)
    {
        var merged = CreateSkeleton(documents, unavailableServices);
        var usedOperationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var document in documents)
        {
            OpenApiComponentMerger.Merge((JsonObject)merged[ComponentsKey]!, document);
            OpenApiPathMerger.Merge((JsonObject)merged[PathsKey]!, document, usedOperationIds);
            MergeTags((JsonArray)merged[TagsKey]!, document);
        }

        merged["x-tagGroups"] = OpenApiTagGroupBuilder.Build(documents);

        return merged.ToJsonString(OutputOptions);
    }

    private static JsonObject CreateSkeleton(IReadOnlyList<SourceDocument> documents, IReadOnlyList<string> unavailableServices) =>
        new()
        {
            ["openapi"] = documents[0].Root["openapi"]?.DeepClone() ?? "3.0.1",
            ["info"] = CreateInfo(unavailableServices),
            [PathsKey] = new JsonObject(),
            [ComponentsKey] = new JsonObject(),
            [TagsKey] = new JsonArray()
        };

    private static JsonObject CreateInfo(IReadOnlyList<string> unavailableServices) =>
        new()
        {
            ["title"] = "LMS API（全部服务）",
            ["version"] = "v1",
            ["description"] = unavailableServices.Count == 0
                ? "网关接口与聚合的全部下游服务接口。"
                : $"网关接口与可用的下游服务接口。以下服务暂时不可用，其接口未列出：{string.Join("、", unavailableServices)}。"
        };

    private static void MergeTags(JsonArray targetTags, SourceDocument document)
    {
        if (document.Root[TagsKey] is not JsonArray tags)
        {
            return;
        }

        var known = targetTags.Select(tag => tag?["name"]?.GetValue<string>()).ToHashSet();
        foreach (var tag in tags.Where(tag => tag?["name"] is not null && known.Add(tag["name"]!.GetValue<string>())))
        {
            targetTags.Add(tag!.DeepClone());
        }
    }
}
