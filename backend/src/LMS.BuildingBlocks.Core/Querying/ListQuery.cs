namespace LMS.BuildingBlocks.Core.Querying;

// 列表查询的通用部分。Search 为已去除首尾空白的文本，空字符串表示不筛选；
// Sort 已包含末尾的稳定标识，保证分页顺序确定。
public sealed record ListQuery(int Page, int PageSize, string Search, IReadOnlyList<SortTerm> Sort)
{
    // 超大页号不会溢出：跳过数量饱和为 int.MaxValue，结果自然为空页。
    public int Skip => (int)Math.Min((long)(Page - 1) * PageSize, int.MaxValue);

    public bool HasSearch => Search.Length > 0;
}
