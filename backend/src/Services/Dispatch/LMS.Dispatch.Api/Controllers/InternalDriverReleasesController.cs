using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Http.Tracing;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Internal;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Drivers.Commands;
using LMS.Dispatch.Application.Drivers.Handlers;
using LMS.Dispatch.Domain.Drivers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 服务间接口：Booking 在行程完成或取消后释放执行该行程的司机。
/// </summary>
/// <remarks>
/// 不经网关；只接受 Booking 签发、仅含 driver-availability:release 范围的服务令牌（DriverReleaseService 策略），员工、司机、客户令牌都不能调用。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.DriverReleaseService)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1")]
public sealed class InternalDriverReleasesController(IReleaseDriverHandler releaseHandler) : ControllerBase
{
    /// <summary>
    /// 释放司机：仍为 ON_TRIP 且不晚于行程结束时写入时恢复为 AVAILABLE；否则不变。重复调用是幂等的。
    /// </summary>
    /// <param name="driverId">司机标识。</param>
    /// <returns>处理后的可用状态；司机不存在为 404。</returns>
    [HttpPost("drivers/{driverId}/availability-releases")]
    [ApiRequestBody(typeof(DriverReleaseRequest))]
    public async Task<IResult> ReleaseAsync(string driverId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<DriverReleaseRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        if (string.IsNullOrWhiteSpace(body.Value.TripId) || body.Value.EndedAt is null)
        {
            return ApiResults.ValidationFailed([new ApiFieldError("tripId", "tripId and endedAt are required")]);
        }

        var context = new CommandContext(AppServiceIdentity.BookingSubject, HttpContext.GetTraceId(), PropertyScope.AllProperties);
        var command = new ReleaseDriverCommand(driverId, body.Value.TripId.Trim(), body.Value.EndedAt.Value, context);

        return OperationResultMapper.ToResult(await releaseHandler.HandleAsync(command, cancellationToken), ToView);
    }

    private static DriverReleaseView ToView(Driver driver) =>
        new(driver.DriverId, driver.Availability is null ? null : WireEnum.ToWire(driver.Availability.Value));
}
