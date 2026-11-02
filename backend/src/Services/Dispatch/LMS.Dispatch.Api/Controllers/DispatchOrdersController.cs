using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.DispatchOrders;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.DispatchOrders;
using LMS.Dispatch.Application.Offers.Handlers;
using LMS.Dispatch.Application.Orders.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 客服派单接口：客户自助单与客服代订单在同一个派单队列里，调度员选择司机发送邀请，司机接受后由 Booking 权威指派。
/// </summary>
/// <remarks>
/// 订单实时读取自 Booking（转发调度员令牌，物业范围与权限由 Booking 重新检查），邀请由 Dispatch 持有。
/// 发送邀请不等于已派单：邀请被司机接受并经 Booking 确认指派后，派单状态才会变为 DRIVER_ASSIGNED。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("客服派单")]
[Route("api/dispatch/staff/v1/dispatch-orders")]
public sealed class DispatchOrdersController(
    IDispatchOrderQueryHandler orderHandler,
    IDispatchCandidateQueryHandler candidateHandler,
    IDispatchOfferHistoryHandler historyHandler,
    ICreateDriverOfferHandler createHandler,
    TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// 分页查询派单队列。
    /// </summary>
    /// <remarks>
    /// 物业范围限定在调用者的授权内；dispatchStatus 筛选、总数与 summaries 使用同一口径。响应不缓存。
    /// </remarks>
    /// <returns>分页的订单；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listDispatchOrders")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DispatchOrderView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> ListAsync([FromQuery] DispatchOrderListRequest request, CancellationToken cancellationToken)
    {
        if (InvalidRange(request.From, request.To) is { } invalid)
        {
            return invalid;
        }

        Response.Headers.CacheControl = "no-store";
        var query = DispatchOrderMapper.ToListQuery(request, CallerContexts.From(HttpContext));

        return PagedResponses.ToResult(await orderHandler.ListAsync(query, cancellationToken), query.List, DispatchOrderMapper.ToView);
    }

    /// <summary>
    /// 各派单状态的订单数。
    /// </summary>
    /// <remarks>
    /// 在除派单状态以外的同一筛选条件下统计，七个状态恒全量返回，用于状态标签上的数量。响应不缓存。
    /// </remarks>
    /// <returns>各状态订单数与总数。</returns>
    [HttpGet("summaries", Name = "getDispatchOrderSummaries")]
    [ProducesResponseType(typeof(ApiEnvelope<DispatchOrderSummaryView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> SummarizeAsync([FromQuery] DispatchOrderSummaryRequest request, CancellationToken cancellationToken)
    {
        if (InvalidRange(request.From, request.To) is { } invalid)
        {
            return invalid;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await orderHandler.SummarizeAsync(DispatchOrderMapper.ToSummaryQuery(request, CallerContexts.From(HttpContext)), cancellationToken);

        return OperationResultMapper.ToResult(result, DispatchOrderMapper.ToView);
    }

    /// <summary>
    /// 派单抽屉详情。
    /// </summary>
    /// <remarks>
    /// 读取订单摘要、备注、Booking 指派、最近一轮邀请，以及 canDispatch 与 blockedReason。读取时会把仍占用中的邀请与 Booking 的权威指派对齐。响应不缓存。
    /// </remarks>
    /// <param name="tripId">行程标识。</param>
    /// <returns>订单详情；不在调用者物业范围内按不存在处理。</returns>
    [HttpGet("{tripId}", Name = "getDispatchOrder")]
    [ProducesResponseType(typeof(ApiEnvelope<DispatchOrderView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> GetAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await orderHandler.GetAsync(tripId, CallerContexts.From(HttpContext), cancellationToken);

        return OperationResultMapper.ToResult(result, DispatchOrderMapper.ToView);
    }

    /// <summary>
    /// 查询订单可选的司机（司机下拉）。
    /// </summary>
    /// <remarks>
    /// 只返回同物业、已启用、可用状态为 AVAILABLE 且此刻没有被邀请占用的司机；search 匹配姓名与车辆文本。
    /// Candidates require a bound vehicle in the trip property with the matching grade and a status other than OUT_OF_SERVICE. vehicleMatch is MATCHED; filtering precedes pagination. Non-dispatchable orders return 409.
    /// Resolve the order pickup time to a business date and period first. Only on-duty drivers scheduled in that period are candidates; before 07:00 uses the previous date's night shift.
    /// </remarks>
    /// <param name="tripId">行程标识。</param>
    /// <returns>分页的候选司机；分页信息在 meta.pagination。</returns>
    [HttpGet("{tripId}/drivers", Name = "listDispatchOrderDrivers")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DispatchCandidateView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> ListDriversAsync(string tripId, [FromQuery] DispatchCandidateListRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var list = request.ToListQuery();

        return PagedResponses.ToResult(await candidateHandler.ListAsync(tripId, list, cancellationToken), list, DispatchOrderMapper.ToView);
    }

    /// <summary>
    /// 查询订单的邀请历史。
    /// </summary>
    /// <param name="tripId">行程标识。</param>
    /// <returns>分页的邀请轮次，默认最近一轮在前。</returns>
    [HttpGet("{tripId}/offers", Name = "listDispatchOrderOffers")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DispatchOfferView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> ListOffersAsync(string tripId, [FromQuery] DispatchOfferListRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var list = request.ToListQuery();
        var serverTime = timeProvider.GetUtcNow();

        return PagedResponses.ToResult(
            await historyHandler.ListAsync(tripId, list, cancellationToken), list, offer => DispatchOrderMapper.ToView(offer, serverTime));
    }

    /// <summary>
    /// 向司机发送邀请（首次派单或重派）。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key；重试同一请求请复用同一个键。X-Expected-Version 可选：提供时必须等于订单当前的派单版本（最近一轮邀请的轮次），否则 409。
    /// 订单须为候补 / 审批中或已确认且没有有效邀请；司机须为同物业、已启用且 AVAILABLE。同一订单、同一司机各自同时只能有一张有效邀请，已过期的旧轮次会先被关闭。
    /// 邀请创建成功只代表已发给司机，不代表司机已接单；客服不能代替司机接受。
    /// </remarks>
    /// <param name="tripId">行程标识。</param>
    /// <returns>新建的邀请（含轮次与到期时间）。</returns>
    [HttpPost("{tripId}/offers", Name = "createDispatchOrderOffer")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateDispatchOfferRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<DispatchOfferView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.DispatchWrite)]
    public async Task<IResult> CreateOfferAsync(string tripId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateDispatchOfferRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, key, version) is { } failure)
        {
            return failure;
        }

        var command = DispatchOrderMapper.ToCreateCommand(tripId, body.Value, key.Value, version.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        var serverTime = timeProvider.GetUtcNow();

        return OperationResultMapper.ToResult(
            await createHandler.HandleAsync(command.Value, cancellationToken), offer => DispatchOrderMapper.ToView(offer, serverTime));
    }

    private static IResult? InvalidRange(DateTimeOffset? from, DateTimeOffset? to) =>
        from is not null && to is not null && to <= from
            ? ApiResults.ValidationFailed([new ApiFieldError("to", "Must be later than from")])
            : null;
}
