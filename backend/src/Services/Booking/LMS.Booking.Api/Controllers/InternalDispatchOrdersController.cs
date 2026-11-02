using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.DispatchOrders;
using LMS.Booking.Api.Mapping.DispatchOrders;
using LMS.Booking.Application.DispatchOrders.Handlers;
using LMS.Booking.Application.DispatchOrders.Models;
using LMS.Booking.Domain.Trips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 服务间接口：Dispatch 的派单队列读取。客户自助单与客服代订单在同一个行程集合内，按订单状态、渠道、物业与接车时间筛选。
/// </summary>
/// <remarks>
/// 不经网关；转发调度员的令牌，Booking 重新检查其 dispatch:read 权限与物业范围。邀请相关的细分由 Dispatch 计算订单集合后传入。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1")]
public sealed class InternalDispatchOrdersController(IDispatchOrderQueryHandler queryHandler) : ControllerBase
{
    private static readonly ListQuerySpec ListSpec =
        new(DispatchOrderSorting.Fields, DispatchOrderSorting.DefaultSort, DispatchOrderSorting.StableField);

    /// <summary>
    /// 分页查询派单队列的订单。
    /// </summary>
    /// <remarks>
    /// 查询参数：page、pageSize、sort、search，以及 propertyId、channel、from、to、group。正文给出订单集合限定，可为空对象。
    /// </remarks>
    /// <returns>分页的订单；分页信息在 meta.pagination。</returns>
    [HttpPost("dispatch-orders/queries")]
    [ApiRequestBody(typeof(DispatchOrderTripIdsRequest))]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        var reader = QueryReader.For(HttpContext);
        var list = reader.ReadList(ListSpec);
        var group = reader.OptionalEnum<DispatchOrderGroup>("group") ?? DispatchOrderGroup.Any;
        var criteria = ReadCriteria(reader, group);
        if (reader.Failure(allowBody: true) is { } failure)
        {
            return failure;
        }

        var body = await JsonRequestBody.ReadAsync<DispatchOrderTripIdsRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var filter = DispatchOrderMapper.ToFilter(criteria, list.Search, body.Value);
        if (filter.Failure is not null)
        {
            return filter.Failure;
        }

        var query = new ListDispatchOrdersQuery(list, HttpContext.GetCaller().Scope, filter.Value);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), list, DispatchOrderMapper.ToView);
    }

    /// <summary>
    /// 统计派单队列的订单数。
    /// </summary>
    /// <remarks>
    /// 查询参数：search、propertyId、channel、from、to。正文给出已确认订单内细分计数所需的订单集合。
    /// </remarks>
    /// <returns>订单计数。</returns>
    [HttpPost("dispatch-order-summaries")]
    [ApiRequestBody(typeof(DispatchOrderCountRequest))]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> CountAsync(CancellationToken cancellationToken)
    {
        var reader = QueryReader.For(HttpContext);
        var search = reader.ReadSearch();
        var criteria = ReadCriteria(reader, DispatchOrderGroup.Any);
        if (reader.Failure(allowBody: true) is { } failure)
        {
            return failure;
        }

        var body = await JsonRequestBody.ReadAsync<DispatchOrderCountRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var query = DispatchOrderMapper.ToCountQuery(criteria, search, body.Value, HttpContext.GetCaller().Scope);
        if (query.Failure is not null)
        {
            return query.Failure;
        }

        return OperationResultMapper.ToResult(await queryHandler.CountAsync(query.Value, cancellationToken), DispatchOrderMapper.ToView);
    }

    /// <summary>
    /// 读取单个订单的派单详情。
    /// </summary>
    /// <param name="tripId">行程标识。</param>
    /// <returns>订单；不属于调用者物业范围时按不存在处理。</returns>
    [HttpGet("dispatch-orders/{tripId}")]
    [RequirePermission(PermissionCodes.DispatchRead)]
    [ProducesResponseType(typeof(ApiEnvelope<DispatchOrderView>), StatusCodes.Status200OK)]
    public async Task<IResult> GetAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(tripId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, DispatchOrderMapper.ToView);
    }

    private static DispatchOrderCriteria ReadCriteria(QueryReader reader, DispatchOrderGroup group)
    {
        var propertyId = reader.OptionalText("propertyId");
        var channel = reader.OptionalEnum<TripChannel>("channel");
        var from = reader.OptionalUtcInstant("from");
        var to = reader.OptionalUtcInstant("to");
        if (from is not null && to is not null && to <= from)
        {
            reader.Reject("to", "Must be later than from");
        }

        return new DispatchOrderCriteria(propertyId, channel, from, to, group);
    }
}
