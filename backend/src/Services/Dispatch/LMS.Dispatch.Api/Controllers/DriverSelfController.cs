using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Self;
using LMS.Dispatch.Api.Contracts.Shifts;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.Self;
using LMS.Dispatch.Api.Mapping.Shifts;
using LMS.Dispatch.Application.Self.Handlers;
using LMS.Dispatch.Application.Shifts.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 司机本人资料与工作状态接口。司机标识只来自令牌绑定，路径、查询串和正文都不接受司机标识。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Driver)]
[Tags("司机本人")]
[Route("api/dispatch/driver/v1/me")]
public sealed class DriverSelfController(
    IDriverSelfQueryHandler queryHandler,
    IChangeDriverAvailabilityHandler availabilityHandler,
    IDriverShiftListQueryHandler shiftQueryHandler) : ControllerBase
{
    /// <summary>
    /// D01 查询本人司机资料与工作状态。
    /// </summary>
    /// <remarks>
    /// 工作状态为 ON_TRIP 时会与 Booking 的权威行程对账；对账来源不可用时返回 503，不返回过期状态。响应不缓存。
    /// </remarks>
    /// <returns>本人资料、工作状态与可执行能力。</returns>
    [HttpGet(Name = "getDriverSelf")]
    [ProducesResponseType(typeof(ApiEnvelope<DriverSelfView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverSelfRead)]
    public async Task<IResult> GetAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await queryHandler.GetAsync(HttpContext.GetPrincipal().EntityId, PrincipalContexts.From(HttpContext), cancellationToken);

        return OperationResultMapper.ToResult(result, DriverSelfMapper.ToView);
    }

    /// <summary>
    /// D14 Query the authenticated driver's shifts.
    /// </summary>
    /// <remarks>
    /// The driver ID comes only from the bearer token. This read-only endpoint cannot list another driver's shifts.
    /// Results are paginated and not cacheable.
    /// </remarks>
    /// <returns>The driver's shift cards and pagination metadata.</returns>
    [HttpGet("shifts", Name = "listOwnDriverShifts")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<ShiftView>>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverSelfRead)]
    public async Task<IResult> ListShiftsAsync([FromQuery] DriverShiftListRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var list = request.ToListQuery();
        var driverId = HttpContext.GetPrincipal().EntityId;
        var result = await shiftQueryHandler.ListAsync(driverId, PropertyScope.AllProperties, list, cancellationToken);

        return PagedResponses.ToResult(result, list, ShiftMapper.ToView);
    }

    /// <summary>
    /// D02 切换本人工作状态。
    /// </summary>
    /// <remarks>
    /// 目标为 AVAILABLE / BREAK / OFF_DUTY；重复设置同一状态返回当前状态。停用 / 暂停的司机，或行程执行中（ON_TRIP）不能切换，返回 422。
    /// </remarks>
    /// <returns>切换后的本人资料。</returns>
    [HttpPatch("availability", Name = "changeDriverAvailability")]
    [ApiRequestBody(typeof(ChangeAvailabilityRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<DriverSelfView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverSelfWrite)]
    public async Task<IResult> ChangeAvailabilityAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<ChangeAvailabilityRequest>(HttpContext, cancellationToken);
        if (body.Failure is { } failure)
        {
            return failure;
        }

        var command = DriverSelfMapper.ToCommand(HttpContext.GetPrincipal().EntityId, body.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await availabilityHandler.HandleAsync(command.Value, cancellationToken), DriverSelfMapper.ToView);
    }
}
