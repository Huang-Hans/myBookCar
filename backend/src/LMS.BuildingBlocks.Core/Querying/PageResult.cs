namespace LMS.BuildingBlocks.Core.Querying;

// 分页结果。TotalItems 是同一授权范围与筛选条件下的总数，不受分页影响。
public sealed record PageResult<T>(IReadOnlyList<T> Items, int TotalItems)
{
    public int TotalPages(int pageSize) => (int)Math.Ceiling(TotalItems / (double)pageSize);
}
