using LMS.BuildingBlocks.Core.Querying;

namespace LMS.Platform.Application.Configuration.Definitions;

// 某个列表配置区域的查询约定：稳定代码、可排序字段、可搜索文本、默认排序。
// 配置区域的行数很少，因此搜索、排序和分页在内存中完成。
public sealed class ConfigurationListDefinition<TItem>(
    Func<TItem, string> keyOf,
    IReadOnlyDictionary<string, Func<TItem, IComparable?>> sortKeys,
    Func<TItem, IEnumerable<string?>> searchTexts,
    string defaultSort,
    string stableSortField)
{
    public Func<TItem, string> KeyOf { get; } = keyOf;

    public IReadOnlyDictionary<string, Func<TItem, IComparable?>> SortKeys { get; } = sortKeys;

    public IReadOnlyCollection<string> SortFields => SortKeys.Keys.ToList();

    public string DefaultSort { get; } = defaultSort;

    public string StableSortField { get; } = stableSortField;

    public bool Matches(TItem item, ListQuery query) =>
        !query.HasSearch || searchTexts(item).Any(text => text?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) == true);
}
