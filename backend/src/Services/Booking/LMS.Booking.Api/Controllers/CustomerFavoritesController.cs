using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.CustomerRecords;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.CustomerRecords;
using LMS.Booking.Application.Customers.Handlers;
using LMS.Booking.Domain.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 客户上车 / 下车地点收藏接口。收藏与员工偏好完全分开，客户只能读写自己的两个分组。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Customer)]
[Tags("客户收藏地点")]
[Route("api/booking/customer/v1/me/location-favorites/{usage}")]
public sealed class CustomerFavoritesController(ILocationFavoritesHandler handler) : ControllerBase
{
    /// <summary>
    /// C2-05 查询某一用途分组的收藏。
    /// </summary>
    /// <remarks>
    /// usage 为 PICKUP 或 DROPOFF。每项带当前可用性：地点已停用或已不存在时 isAvailable=false，收藏仍保留；目录暂时不可读时省略该字段。
    /// 从未保存过为空列表（version=0），不写库。响应不缓存。
    /// </remarks>
    /// <param name="usage">PICKUP 或 DROPOFF。</param>
    /// <returns>该分组的收藏。</returns>
    [HttpGet(Name = "getCustomerLocationFavorites")]
    [ProducesResponseType(typeof(ApiEnvelope<LocationFavoritesView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerFavoritesRead)]
    public async Task<IResult> GetAsync(string usage, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        if (!WireEnum.TryParse<FavoriteUsage>(usage, out var parsed))
        {
            return InvalidUsage();
        }

        Response.Headers.CacheControl = "no-store";
        var result = await handler.GetAsync(HttpContext.GetPrincipal().EntityId, parsed, cancellationToken);

        return OperationResultMapper.ToResult(result, CustomerRecordMapper.ToView);
    }

    /// <summary>
    /// C2-06 整体替换某一用途分组的收藏。
    /// </summary>
    /// <remarks>
    /// 空数组清空该分组，另一分组不受影响。每项必须是启用的目录地点（自由文本不能冒充地点标识；自定义地址的受约束解析尚未提供）。
    /// 同一分组内不允许重复；条目数不得超过部署配置的上限（Favorites:MaxItemsPerUsage，默认 50）。
    /// 可选 X-Expected-Version：提供时必须等于当前版本（从未保存过为 0），否则 409。内容没有变化时不写入。
    /// </remarks>
    /// <param name="usage">PICKUP 或 DROPOFF。</param>
    /// <returns>替换后的该分组收藏。</returns>
    [HttpPut(Name = "replaceCustomerLocationFavorites")]
    [ApiRequestBody(typeof(ReplaceLocationFavoritesRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<LocationFavoritesView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerFavoritesWrite)]
    public async Task<IResult> ReplaceAsync(string usage, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<ReplaceLocationFavoritesRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        if (!WireEnum.TryParse<FavoriteUsage>(usage, out var parsed))
        {
            return InvalidUsage();
        }

        var command = CustomerRecordMapper.ToCommand(
            HttpContext.GetPrincipal().EntityId, parsed, version.Value, body.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await handler.ReplaceAsync(command.Value, cancellationToken), CustomerRecordMapper.ToView);
    }

    private static IResult InvalidUsage() =>
        ApiResults.ValidationFailed([new ApiFieldError("usage", $"Must be one of: {string.Join(", ", WireEnum.WireNames<FavoriteUsage>())}")]);
}
