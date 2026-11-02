using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Shifts;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.Shifts;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// Driver scheduling. Property and vehicle follow the driver's current records.
/// </summary>
/// <remarks>
/// 看板按澳门业务日期返回早班 07:00–15:00、午班 15:00–23:00、夜班 23:00–次日 07:00。
/// 夜班挂在开始那天。地点筛选使用物业代码；卡片上的物业名称由调用方的物业目录解析。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("排班")]
[Route("api/dispatch/staff/v1/shifts")]
public sealed class ShiftsController(
    IShiftBoardQueryHandler queryHandler,
    ICreateShiftHandler createHandler,
    IUpdateShiftHandler updateHandler,
    IAssignShiftVehicleHandler vehicleHandler) : ControllerBase
{
    /// <summary>
    /// SH01 Query shifts by business date or pickup time.
    /// </summary>
    /// <remarks>
    /// With pickupAt (ISO 8601 UTC), resolve the business date and period before reading on-duty drivers.
    /// Before 07:00 belongs to the previous date's night shift. Only the matching period is returned.
    /// Without pickupAt, return all three periods for date (default today). Property scope is enforced. No caching.
    /// </remarks>
    /// <returns>当天的早、午、夜三列班次。</returns>
    [HttpGet("board", Name = "getShiftBoard")]
    [ProducesResponseType(typeof(ApiEnvelope<ShiftBoardView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> GetBoardAsync([FromQuery] ShiftBoardRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var query = ShiftMapper.ToBoardQuery(request, CallerContexts.From(HttpContext));
        if (query.Failure is not null) return query.Failure;
        var result = await queryHandler.GetBoardAsync(query.Value, cancellationToken);

        return OperationResultMapper.ToResult(result, ShiftMapper.ToView);
    }

    /// <summary>
    /// SH08 Query shift boards for an inclusive business-date range.
    /// </summary>
    /// <remarks>
    /// Returns every date and all three periods, including empty days. The range is limited to 31 dates
    /// within the caller's property scope. Shifts and driver names are loaded in batches. Responses are not cached.
    /// </remarks>
    [HttpGet("calendar", Name = "listShiftCalendar")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<ShiftBoardView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> GetCalendarAsync([FromQuery] ShiftCalendarRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var query = ShiftMapper.ToCalendarQuery(request, CallerContexts.From(HttpContext));
        if (query.Failure is not null)
        {
            return query.Failure;
        }

        var result = await queryHandler.GetCalendarAsync(query.Value, cancellationToken);

        return OperationResultMapper.ToResult(result, boards => boards.Select(ShiftMapper.ToView).ToList());
    }

    /// <summary>
    /// SH07 查询未来 15 天的排班列表。
    /// </summary>
    /// <remarks>
    /// 从澳门业务当天起，含当天，连续 15 个日期。没有班次的日期也返回。物业范围限定在调用者授权内。响应不缓存。
    /// </remarks>
    /// <returns>15 天的排班，按日期升序。</returns>
    [HttpGet("upcoming", Name = "listUpcomingShifts")]
    [ProducesResponseType(typeof(ApiEnvelope<ShiftUpcomingView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> GetUpcomingAsync([FromQuery] ShiftUpcomingRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await queryHandler.GetUpcomingAsync(ShiftMapper.ToUpcomingQuery(request, CallerContexts.From(HttpContext)), cancellationToken);

        return OperationResultMapper.ToResult(result, ShiftMapper.ToView);
    }

    /// <summary>
    /// SH02 查询班次详情。
    /// </summary>
    /// <param name="shiftId">班次标识。</param>
    /// <returns>班次卡片。不属于调用者物业范围时按不可见处理。</returns>
    [HttpGet("{shiftId}", Name = "getShift")]
    [ProducesResponseType(typeof(ApiEnvelope<ShiftView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> GetAsync(string shiftId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await queryHandler.GetAsync(shiftId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, ShiftMapper.ToView);
    }

    /// <summary>
    /// SH03 新增班次。
    /// </summary>
    /// <remarks>
    /// Requires Idempotency-Key. Requests contain date, driver, period, status and role. Property follows the current driver record.
    /// A driver may have different periods on one business date; the same period cannot be scheduled twice.
    /// </remarks>
    /// <returns>新建的班次。</returns>
    [HttpPost(Name = "createShift")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateShiftRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<ShiftView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.DispatchWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateShiftRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = ShiftMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), ShiftMapper.ToView);
    }

    /// <summary>
    /// SH04 更新班次角色或到班标记。
    /// </summary>
    /// <param name="shiftId">班次标识。</param>
    /// <returns>更新后的班次。</returns>
    [HttpPatch("{shiftId}", Name = "updateShift")]
    [ApiRequestBody(typeof(PatchShiftRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<ShiftView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchWrite)]
    public async Task<IResult> UpdateAsync(string shiftId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<PatchShiftRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = ShiftMapper.ToUpdateCommand(shiftId, version.Value, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await updateHandler.HandleAsync(command.Value, cancellationToken), ShiftMapper.ToView);
    }

    /// <summary>
    /// SH05 Legacy adapter: change the driver's bound vehicle through a visible shift.
    /// </summary>
    /// <param name="shiftId">班次标识。</param>
    /// <remarks>Updates the driver's vehicle binding for all shifts. No vehicle is stored on the shift.</remarks>
    /// <returns>The shift with the driver's current vehicle.</returns>
    [HttpPut("{shiftId}/vehicle", Name = "assignShiftVehicle")]
    [RequirePermission(PermissionCodes.DriversWrite)]
    [ApiRequestBody(typeof(AssignShiftVehicleRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<ShiftView>), StatusCodes.Status200OK)]
    public async Task<IResult> AssignVehicleAsync(string shiftId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<AssignShiftVehicleRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = ShiftMapper.ToAssignCommand(shiftId, version.Value, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await vehicleHandler.AssignAsync(command.Value, cancellationToken), ShiftMapper.ToView);
    }

    /// <summary>
    /// SH06 Legacy adapter: clear the driver's vehicle binding for all shifts.
    /// </summary>
    /// <param name="shiftId">班次标识。</param>
    /// <returns>The shift with no bound driver vehicle.</returns>
    [HttpDelete("{shiftId}/vehicle", Name = "clearShiftVehicle")]
    [RequirePermission(PermissionCodes.DriversWrite)]
    [ProducesResponseType(typeof(ApiEnvelope<ShiftView>), StatusCodes.Status200OK)]
    public async Task<IResult> ClearVehicleAsync(string shiftId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        var command = new ClearShiftVehicleCommand(shiftId, version.Value, CallerContexts.From(HttpContext));

        return OperationResultMapper.ToResult(await vehicleHandler.ClearAsync(command, cancellationToken), ShiftMapper.ToView);
    }
}
