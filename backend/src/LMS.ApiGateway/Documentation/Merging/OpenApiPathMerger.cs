using System.Text.Json.Nodes;

namespace LMS.ApiGateway.Documentation.Merging;

// 合并 paths：各服务的路径带服务段，通常互不重叠；万一同一路径同一方法重复，以先加入的为准。
internal static class OpenApiPathMerger
{
    private const string PathsKey = "paths";
    private const string OperationIdKey = "operationId";

    private static readonly HashSet<string> HttpMethods =
        new(["get", "put", "post", "delete", "options", "head", "patch", "trace"], StringComparer.OrdinalIgnoreCase);

    public static void Merge(JsonObject targetPaths, SourceDocument document, HashSet<string> usedOperationIds)
    {
        if (document.Root[PathsKey] is not JsonObject paths)
        {
            return;
        }

        foreach (var (path, item) in paths.ToList())
        {
            if (item is JsonObject pathItem)
            {
                MergePathItem(targetPaths, path, pathItem, document.Source.Key, usedOperationIds);
            }
        }
    }

    private static void MergePathItem(
        JsonObject targetPaths,
        string path,
        JsonObject pathItem,
        string serviceKey,
        HashSet<string> usedOperationIds)
    {
        var targetItem = targetPaths[path] as JsonObject ?? new JsonObject();
        targetPaths[path] = targetItem;
        foreach (var (member, node) in pathItem.ToList())
        {
            if (HttpMethods.Contains(member) && node is JsonObject operation && !targetItem.ContainsKey(member))
            {
                targetItem[member] = CloneOperation(operation, serviceKey, usedOperationIds);
            }
        }
    }

    // operationId 在整份文档内必须唯一；冲突时加服务前缀，客户端生成器据此区分。
    private static JsonNode CloneOperation(JsonObject operation, string serviceKey, HashSet<string> usedOperationIds)
    {
        var clone = (JsonObject)operation.DeepClone();
        if (clone[OperationIdKey] is JsonValue idValue && idValue.TryGetValue<string>(out var operationId))
        {
            var uniqueId = usedOperationIds.Contains(operationId) ? $"{serviceKey}_{operationId}" : operationId;
            usedOperationIds.Add(uniqueId);
            clone[OperationIdKey] = uniqueId;
        }

        return clone;
    }
}
