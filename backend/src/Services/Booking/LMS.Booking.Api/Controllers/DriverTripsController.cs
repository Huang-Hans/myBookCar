using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.DriverTrips;
using LMS.Booking.Api.Contracts.TripLocations;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.DriverTrips;
using LMS.Booking.Api.Mapping.TripLocations;
using LMS.Booking.Application.DriverTrips.Handlers;
using LMS.Booking.Application.TripLocations.Handlers;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 司机本人的行程接口（当前行程、历史、详情与履约动作）。司机标识只来自令牌绑定；
/// 他人的行程、未指派给本人的行程与不存在不作区分，一律 404。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Driver)]
[Tags("司机行程")]
[Route("api/booking/driver/v1/me/trips")]
public sealed class DriverTripsController(
    IDriverTripQueryHandler queryHandler,
    IRecordTripProgressHandler progressHandler,
    IRecordTripLocationsHandler locationHandler) : ControllerBase
{
    /// <summary>
    /// D05 查询本人行程（当前或历史）。
    /// </summary>
    /// <remarks>
    /// view=ACTIVE 返回当前有效行程；无行程时为空列表（真实空结果），查询失败返回错误而不是空列表。
    /// view=HISTORY 返回已完成或已取消且归属本人的行程，可用 status 筛选终态，默认按结束时间倒序，稳定分页。
    /// 已拒绝或超时的邀请不是行程，不进入该列表。存量行程没有司机指派，不会归属任何司机。响应不缓存。
    /// </remarks>
    /// <returns>分页的行程列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listDriverTrips")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DriverTripSummaryView>>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverTripsRead)]
    public async Task<IResult> ListAsync([FromQuery] DriverTripListRequest request, CancellationToken cancellationToken)
    {
        if (request.Status is { } status && status is not (TripStatus.Completed or TripStatus.Cancelled))
        {
            return ApiResults.ValidationFailed([new ApiFieldError("status", "Must be COMPLETED or CANCELLED")]);
        }

        Response.Headers.CacheControl = "no-store";
        var list = request.ToListQuery();
        var query = new ListDriverTripsQuery(list, HttpContext.GetPrincipal().EntityId, request.View, request.Status);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), list, DriverTripMapper.ToSummary);
    }

    /// <summary>
    /// D06 查询本人行程详情。
    /// </summary>
    /// <remarks>
    /// 返回乘客摘要、特殊说明、履约实际时间、轨迹和服务端给出的 allowedActions。没有真实联络渠道时 contact.canContactPassenger 为 false。响应不缓存。
    /// </remarks>
    /// <param name="tripId">行程标识。</param>
    /// <returns>行程详情。</returns>
    [HttpGet("{tripId}", Name = "getDriverTrip")]
    [ProducesResponseType(typeof(ApiEnvelope<DriverTripDetailView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverTripsRead)]
    public async Task<IResult> GetAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await queryHandler.GetAsync(tripId, HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, DriverTripMapper.ToDetail);
    }

    /// <summary>
    /// D07 提交履约动作。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// 目标状态由服务端状态机决定：只有 allowedActions 中的下一动作可提交，其余返回 409。
    /// 同一幂等键重复提交同一动作返回当前行程（仍为 201）；同一键提交不同动作返回 409。
    /// 只有服务端收到并成功提交的动作才是权威事实。
    /// </remarks>
    /// <param name="tripId">行程标识。</param>
    /// <returns>动作提交后的行程详情。</returns>
    [HttpPost("{tripId}/progress-events", Name = "createDriverTripProgressEvent")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(ProgressEventRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<DriverTripDetailView>), StatusCodes.Status201Created)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverTripsWrite)]
    public async Task<IResult> RecordProgressAsync(string tripId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<ProgressEventRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var driverId = HttpContext.GetPrincipal().EntityId;
        var command = DriverTripMapper.ToProgressCommand(tripId, driverId, key.Value, body.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await progressHandler.HandleAsync(command.Value, cancellationToken), DriverTripMapper.ToDetail);
    }

    /// <summary>
    /// D08 上报本人行程的实时位置。
    /// </summary>
    /// <remarks>
    /// 只在共享窗口内接受（司机已出发且行程未结束），窗口外返回 409。坐标系固定为 GCJ02，一次 1 至 50 个点。
    /// 只保存序号最大的点作为最新位置；序号不大于已存值的点被忽略，接口仍返回 200，可安全重试。
    /// 新位置按行程限流（约每 3 秒一次）推送给正在查看该行程的客户与客服。
    /// </remarks>
    /// <param name="tripId">行程标识。</param>
    /// <returns>服务端已保存的最大序号。</returns>
    [HttpPost("{tripId}/locations", Name = "createDriverTripLocations")]
    [ApiRequestBody(typeof(TripLocationUploadRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<TripLocationUploadView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverTripsWrite)]
    public async Task<IResult> RecordLocationsAsync(string tripId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<TripLocationUploadRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var command = TripLocationMapper.ToCommand(tripId, HttpContext.GetPrincipal().EntityId, body.Value);
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await locationHandler.HandleAsync(command.Value, cancellationToken), TripLocationMapper.ToUploadView);
    }
}
