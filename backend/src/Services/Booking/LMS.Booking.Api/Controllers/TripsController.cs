using LMS.Booking.Application.SelfService.Options;
using Microsoft.Extensions.Options;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.TripLocations;
using LMS.Booking.Api.Contracts.Trips;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.TripLocations;
using LMS.Booking.Api.Mapping.Trips;
using LMS.Booking.Application.TripLocations.Handlers;
using LMS.Booking.Application.Trips.Handlers;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 客服代订行程接口（HA-01 建单 / HA-04 追踪 / HA-05 历史 / HA-10 修改与取消）。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("客服行程")]
public sealed class TripsController(
    ITripQueryHandler queryHandler,
    ICreateTripHandler createHandler,
    IUpdateTripHandler updateHandler,
    ICancelTripHandler cancelHandler,
    ITripTrackQueryHandler trackHandler, IOptions<SelfServiceOptions> products) : ControllerBase
{
    private static readonly ListQuerySpec ListSpec =
        new(TripSorting.Fields, TripSorting.DefaultSort, TripSorting.StableField);

    /// <summary>
    /// 分页查询行程（追踪与历史共用）。
    /// </summary>
    /// <remarks>
    /// 查询参数：page、pageSize、sort、search，以及 propertyId、status、from、to、redemptionState、channel。
    /// </remarks>
    [HttpGet("api/booking/staff/v1/trips", Name = "listTrips")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<TripView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BookingsRead)]
    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        var reader = QueryReader.For(HttpContext);
        var list = reader.ReadList(ListSpec);
        var propertyId = reader.OptionalText("propertyId");
        var status = reader.OptionalEnum<TripStatus>("status");
        var channel = reader.OptionalEnum<TripChannel>("channel");
        var from = reader.OptionalUtcInstant("from");
        var to = reader.OptionalUtcInstant("to");
        var redemptionState = reader.OptionalText("redemptionState");
        if (reader.Failure() is { } failure)
        {
            return failure;
        }

        if (from is not null && to is not null && to <= from)
        {
            return ApiResults.ValidationFailed([new ApiFieldError("to", "Must be later than from")]);
        }

        var query = new ListTripsQuery(list, HttpContext.GetCaller().Scope, propertyId, status, from, to, redemptionState, channel);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), list, TripMapper.ToView);
    }

    /// <summary>
    /// 创建代订行程。需要 Idempotency-Key；同键同内容重放同样 201。
    /// </summary>
    [HttpPost("api/booking/staff/v1/trips", Name = "createTrip")]
    [ApiRequestBody(typeof(CreateTripRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<TripView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.BookingsWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateTripRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = TripMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext), products.Value);
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), TripMapper.ToView);
    }

    /// <summary>
    /// 查询行程详情。
    /// </summary>
    [HttpGet("api/booking/staff/v1/trips/{tripId}", Name = "getTrip")]
    [ProducesResponseType(typeof(ApiEnvelope<TripView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BookingsRead)]
    public async Task<IResult> GetAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(tripId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, TripMapper.ToView);
    }

    /// <summary>
    /// 查询行程时间线：创建、每次修改、司机指派与履约、取消，来自订单已记录的真实事件，按发生时间升序。
    /// </summary>
    [HttpGet("api/booking/staff/v1/trips/{tripId}/events", Name = "listTripEvents")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<TripTimelineEventView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BookingsRead)]
    public async Task<IResult> ListEventsAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(tripId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, TripTimelineMapper.ToTimeline);
    }

    /// <summary>
    /// 查询行程行驶轨迹：司机出发后上报的全部定位点（GCJ-02），按序号升序；已结束的行程在保留期内仍可查询。
    /// 超过 2000 个点时等间隔抽样并返回 sampled=true。afterSeq 用于与实时推送衔接时增量补拉。
    /// </summary>
    [HttpGet("api/booking/staff/v1/trips/{tripId}/track", Name = "getTripTrack")]
    [ProducesResponseType(typeof(ApiEnvelope<TripTrackView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BookingsRead)]
    public async Task<IResult> GetTrackAsync(string tripId, [FromQuery] TripTrackRequest request, CancellationToken cancellationToken)
    {
        var result = await trackHandler.GetForStaffAsync(tripId, request.AfterSeq, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, TripLocationMapper.ToView);
    }

    /// <summary>
    /// 局部更新行程。X-Expected-Version 可选且目前不参与比较；仅 PENDING~DRIVER_ASSIGNED 可改。
    /// 可带 modificationReason（GUEST_REQUEST / SCHEDULE_CHANGE）与 modificationComments，记入修改记录并在详情的 lastModification 与时间线中返回。
    /// </summary>
    [HttpPatch("api/booking/staff/v1/trips/{tripId}", Name = "updateTrip")]
    [ApiRequestBody(typeof(PatchTripRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<TripView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BookingsWrite)]
    public async Task<IResult> UpdateAsync(string tripId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<PatchTripRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = TripMapper.ToUpdateCommand(tripId, version.Value, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await updateHandler.HandleAsync(command.Value, cancellationToken), TripMapper.ToView);
    }

    /// <summary>
    /// 取消行程。需要 Idempotency-Key，X-Expected-Version 可选；原因码来自上传取消原因清单，comments 可选。
    /// </summary>
    [HttpPost("api/booking/staff/v1/trips/{tripId}/cancellation", Name = "cancelTrip")]
    [ApiRequestBody(typeof(CancelTripRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<TripView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BookingsWrite)]
    public async Task<IResult> CancelAsync(string tripId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CancelTripRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, version, key) is { } failure)
        {
            return failure;
        }

        var command = TripMapper.ToCancelCommand(tripId, version.Value, body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await cancelHandler.HandleAsync(command.Value, cancellationToken), TripMapper.ToView);
    }

    /// <summary>
    /// 取消前政策展示（物业政策引用与当前状态）。
    /// </summary>
    [HttpGet("api/booking/staff/v1/trips/{tripId}/cancellation-policy", Name = "getTripCancellationPolicy")]
    [ProducesResponseType(typeof(ApiEnvelope<TripCancellationPolicyView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BookingsRead)]
    public async Task<IResult> GetCancellationPolicyAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetCancellationPolicyAsync(tripId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, TripMapper.ToView);
    }
}
