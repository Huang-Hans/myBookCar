using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.DriverTrips;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.DriverTrips;
using LMS.Booking.Application.DriverTrips.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 服务间接口：Dispatch 的接单协作。读取待分配行程、请求权威指派、核对司机当前有效行程。
/// </summary>
/// <remarks>
/// 不经网关；转发终端用户的令牌，Booking 重新检查其权限与本人身份。指派以邀请标识幂等，重试同一邀请不会产生第二笔指派。
/// </remarks>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1")]
public sealed class InternalDriverAssignmentsController(
    IDriverTripQueryHandler queryHandler,
    IAssignDriverHandler assignHandler) : ControllerBase
{
    /// <summary>
    /// 读取待分配行程摘要（调度员令牌，按物业范围限定）；isAssignable 为 false 表示行程当前不可分配。
    /// </summary>
    /// <param name="tripId">行程标识。</param>
    /// <returns>待分配行程摘要。</returns>
    [HttpGet("trips/{tripId}/assignment-candidate")]
    [Authorize(Policy = AuthorizationPolicies.Staff)]
    [RequirePermission(PermissionCodes.BookingsRead)]
    public async Task<IResult> GetCandidateAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetForAssignmentAsync(tripId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, DriverTripMapper.ToCandidate);
    }

    /// <summary>
    /// 条件指派（司机令牌）：行程须为候补 / 审批中或已确认且尚未指派，司机同时只能有一个执行中的行程；已被本邀请指派时返回同一结果。
    /// </summary>
    /// <param name="tripId">行程标识。</param>
    /// <returns>指派后的行程引用；冲突为 409。</returns>
    [HttpPost("trips/{tripId}/driver-assignments")]
    [ApiRequestBody(typeof(DriverAssignmentRequest))]
    [Authorize(Policy = AuthorizationPolicies.Driver)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverTripsWrite)]
    public async Task<IResult> AssignAsync(string tripId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<DriverAssignmentRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var command = DriverTripMapper.ToAssignCommand(tripId, HttpContext.GetPrincipal().EntityId, body.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await assignHandler.HandleAsync(command.Value, cancellationToken), DriverTripMapper.ToReference);
    }

    /// <summary>
    /// 核对司机当前有效（已指派未终态）的行程；没有为 404。
    /// </summary>
    /// <returns>行程引用。</returns>
    [HttpGet("driver-active-trip")]
    [Authorize(Policy = AuthorizationPolicies.Driver)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverTripsRead)]
    public async Task<IResult> GetActiveTripAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetActiveAsync(HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, DriverTripMapper.ToReference);
    }

    /// <summary>
    /// 核对指定行程是否归属司机本人（含已完成 / 已取消的行程），供事故报告引用行程；不是本人的行程与不存在不作区分，一律 404。
    /// </summary>
    /// <param name="tripId">行程标识。</param>
    /// <returns>行程引用。</returns>
    [HttpGet("driver-trips/{tripId}")]
    [Authorize(Policy = AuthorizationPolicies.Driver)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverTripsRead)]
    public async Task<IResult> GetDriverTripAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(tripId, HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, DriverTripMapper.ToReference);
    }
}
