using System.Text.Json.Nodes;

namespace LMS.ApiGateway.Documentation.Merging;

// 合并 components：同名且内容相同的组件复用；同名但内容不同的加服务前缀后并存。
internal static class OpenApiComponentMerger
{
    private const string ComponentsKey = "components";

    // 安全方案按名称在 security 要求里引用，不能改名：同名时以先加入的为准。
    private const string SecuritySchemesSection = "securitySchemes";

    // 先规划重命名并改写该文档内的引用，再把组件并入目标；调用方随后合并该文档的 paths。
    public static void Merge(JsonObject target, SourceDocument document)
    {
        if (document.Root[ComponentsKey] is not JsonObject components)
        {
            return;
        }

        var renames = PlanRenames(target, components, document.Source.Key);
        ApplyRenames(document.Root, components, renames);
        AddAll(target, components);
    }

    private static List<ComponentRename> PlanRenames(JsonObject target, JsonObject components, string prefix)
    {
        var renames = new List<ComponentRename>();
        foreach (var (section, items) in Sections(components))
        {
            renames.AddRange(PlanSection(target[section] as JsonObject, section, items, prefix));
        }

        return renames;
    }

    private static IEnumerable<ComponentRename> PlanSection(JsonObject? targetItems, string section, JsonObject items, string prefix)
    {
        if (targetItems is null || section == SecuritySchemesSection)
        {
            yield break;
        }

        foreach (var (name, node) in items)
        {
            if (targetItems.TryGetPropertyValue(name, out var existing) && !JsonNode.DeepEquals(existing, node))
            {
                yield return new ComponentRename(section, name, UniqueName(targetItems, items, prefix, name));
            }
        }
    }

    private static string UniqueName(JsonObject targetItems, JsonObject items, string prefix, string name)
    {
        var baseName = $"{ToPascalCase(prefix)}{name}";
        var candidate = baseName;
        for (var suffix = 2; targetItems.ContainsKey(candidate) || items.ContainsKey(candidate); suffix++)
        {
            candidate = $"{baseName}{suffix}";
        }

        return candidate;
    }

    private static void ApplyRenames(JsonObject root, JsonObject components, List<ComponentRename> renames)
    {
        if (renames.Count == 0)
        {
            return;
        }

        foreach (var rename in renames)
        {
            RenameKey((JsonObject)components[rename.Section]!, rename.OldName, rename.NewName);
        }

        OpenApiReferenceRewriter.Rewrite(root, renames.ToDictionary(rename => rename.OldReference, rename => rename.NewReference));
    }

    private static void RenameKey(JsonObject items, string oldName, string newName)
    {
        var node = items[oldName];
        items.Remove(oldName);
        items[newName] = node;
    }

    private static void AddAll(JsonObject target, JsonObject components)
    {
        foreach (var (section, items) in Sections(components))
        {
            var targetItems = target[section] as JsonObject ?? new JsonObject();
            target[section] = targetItems;
            AddMissing(targetItems, items);
        }
    }

    private static void AddMissing(JsonObject targetItems, JsonObject items)
    {
        foreach (var (name, node) in items.ToList())
        {
            if (!targetItems.ContainsKey(name))
            {
                targetItems[name] = node?.DeepClone();
            }
        }
    }

    private static IEnumerable<(string Section, JsonObject Items)> Sections(JsonObject components) =>
        components.Where(section => section.Value is JsonObject).Select(section => (section.Key, (JsonObject)section.Value!));

    private static string ToPascalCase(string value) => char.ToUpperInvariant(value[0]) + value[1..];
}
