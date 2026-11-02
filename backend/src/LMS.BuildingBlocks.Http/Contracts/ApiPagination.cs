namespace LMS.BuildingBlocks.Http.Contracts;

/// <summary>
/// 分页信息。
/// </summary>
/// <param name="CurrentPage">请求页号，从 1 开始。</param>
/// <param name="PageSize">每页条数，取值为 10、20 或 50。</param>
/// <param name="TotalItems">同一授权与筛选范围内的真实总数，不因翻页改变。</param>
/// <param name="TotalPages">总页数；无结果为 0。请求页超过总页数时返回空 data，总数保持真实值。</param>
public sealed record ApiPagination(int CurrentPage, int PageSize, int TotalItems, int TotalPages);
