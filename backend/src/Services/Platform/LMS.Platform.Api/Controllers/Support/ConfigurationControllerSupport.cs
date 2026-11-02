using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.Platform.Application.Configuration.Handlers;

namespace LMS.Platform.Api.Controllers.Support;

// 只读配置控制器共有的请求处理：严格读取查询串，调用用例，再映射为统一响应。
internal static class ConfigurationControllerSupport
{
    public static async Task<IResult> ListAsync<TItem, TView>(
        IConfigurationListHandler<TItem> handler,
        ListQuery list,
        Func<TItem, bool>? filter,
        Func<TItem, TView> toView,
        CancellationToken cancellationToken) =>
        PagedResponses.ToResult(await handler.ListAsync(list, filter, cancellationToken), list, toView);

    // 不分页的卡片或矩阵：没有 page / pageSize / sort / search，只接受调用方给出的过滤参数。
    public static async Task<IResult> ListAllAsync<TItem, TView>(
        IConfigurationListHandler<TItem> handler,
        Func<TItem, bool>? filter,
        Func<TItem, TView> toView,
        CancellationToken cancellationToken)
    {
        var result = await handler.ListAllAsync(filter, cancellationToken);

        return result.IsSuccess
            ? ApiResults.Ok(result.Value!.Select(toView).ToList())
            : OperationResultMapper.ToFailure(result);
    }

    // isActive 未给出时不过滤。
    public static Func<TItem, bool>? ActiveFilter<TItem>(bool? isActive, Func<TItem, bool> isActiveOf) =>
        isActive is null ? null : item => isActiveOf(item) == isActive.Value;

    public static async Task<IResult> FindAsync<TItem, TView>(
        HttpContext http,
        string key,
        IConfigurationListHandler<TItem> handler,
        Func<TItem, TView> toView,
        Func<TItem, bool>? filter,
        CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(http) is { } failure)
        {
            return failure;
        }

        return OperationResultMapper.ToResult(await handler.FindAsync(key, filter, cancellationToken), toView);
    }

    public static async Task<IResult> GetDocumentAsync<TModel, TView>(
        HttpContext http,
        IConfigurationDocumentHandler<TModel> handler,
        Func<TModel, TView> toView,
        CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(http) is { } failure)
        {
            return failure;
        }

        return OperationResultMapper.ToResult(await handler.GetAsync(cancellationToken), toView);
    }
}
