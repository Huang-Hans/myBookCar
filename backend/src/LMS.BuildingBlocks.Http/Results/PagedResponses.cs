using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Http.Contracts;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Results;

public static class PagedResponses
{
    public static IResult ToResult<TItem, TView>(PageResult<TItem> page, ListQuery query, Func<TItem, TView> toView)
    {
        var views = page.Items.Select(toView).ToList();
        var pagination = new ApiPagination(query.Page, query.PageSize, page.TotalItems, page.TotalPages(query.PageSize));

        return ApiResults.OkPage<TView>(views, pagination);
    }

    // 用例成功时输出分页列表，否则按结果分类输出失败响应。
    public static IResult ToResult<TItem, TView>(
        OperationResult<PageResult<TItem>> result,
        ListQuery query,
        Func<TItem, TView> toView) =>
        result.IsSuccess ? ToResult(result.Value!, query, toView) : OperationResultMapper.ToFailure(result);
}
