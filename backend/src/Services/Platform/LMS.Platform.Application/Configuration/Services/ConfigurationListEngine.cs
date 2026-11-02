using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.Configuration.Definitions;

namespace LMS.Platform.Application.Configuration.Services;

// 配置列表的内存查询：过滤 → 搜索 → 排序（空值固定置后）→ 分页。
internal static class ConfigurationListEngine
{
    public static PageResult<TItem> Page<TItem>(
        IReadOnlyList<TItem> items,
        ConfigurationListDefinition<TItem> definition,
        ListQuery query,
        Func<TItem, bool>? filter)
    {
        var matches = items
            .Where(item => (filter?.Invoke(item) ?? true) && definition.Matches(item, query))
            .ToList();
        var page = Sort(matches, definition, query.Sort).Skip(query.Skip).Take(query.PageSize).ToList();

        return new PageResult<TItem>(page, matches.Count);
    }

    private static IOrderedEnumerable<TItem> Sort<TItem>(
        List<TItem> items,
        ConfigurationListDefinition<TItem> definition,
        IReadOnlyList<SortTerm> terms)
    {
        IOrderedEnumerable<TItem>? ordered = null;
        foreach (var term in terms)
        {
            ordered = Chain(ordered, items, definition.SortKeys[term.Field], term.IsDescending);
        }

        return ordered ?? items.OrderBy(definition.KeyOf, StringComparer.Ordinal);
    }

    private static IOrderedEnumerable<TItem> Chain<TItem>(
        IOrderedEnumerable<TItem>? ordered,
        List<TItem> items,
        Func<TItem, IComparable?> key,
        bool isDescending)
    {
        var nullsLast = ordered is null ? items.OrderBy(item => key(item) is null) : ordered.ThenBy(item => key(item) is null);

        return isDescending
            ? nullsLast.ThenByDescending(key, SortKeyComparer.Instance)
            : nullsLast.ThenBy(key, SortKeyComparer.Instance);
    }
}
