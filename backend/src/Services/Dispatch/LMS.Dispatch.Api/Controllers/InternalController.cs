using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Internal;
using LMS.Dispatch.Application.Drivers.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 服务间接口：Booking 审批前核对司机状态。
/// </summary>
/// <remarks>
/// 不经网关；转发终端用户的令牌，并按其物业范围限定结果。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1")]
public sealed class InternalController(ICheckDriverStatusesHandler checkDriverStatusesHandler) : ControllerBase
{
    private const int MaxDriverIds = 500;

    /// <summary>
    /// 批量核对司机状态。
    /// </summary>
    /// <returns>请求中可见司机的状态列表；不可见或不存在的不出现在结果里。</returns>
    [HttpPost("driver-status-checks")]
    [RequirePermission(PermissionCodes.ApprovalsRead)]
    public async Task<IResult> CheckDriverStatusesAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<DriverStatusCheckRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var driverIds = body.Value.DriverIds;
        if (driverIds is null || driverIds.Count > MaxDriverIds || driverIds.Any(string.IsNullOrWhiteSpace))
        {
            return ApiResults.ValidationFailed([new ApiFieldError("driverIds", $"Must contain 0 to {MaxDriverIds} non-blank driver ids")]);
        }

        var distinctIds = driverIds.Select(id => id!.Trim()).Distinct(StringComparer.Ordinal).ToList();
        var found = await checkDriverStatusesHandler.HandleAsync(distinctIds, HttpContext.GetCaller().Scope, cancellationToken);

        return ApiResults.Ok(found
            .Select(info => new DriverStatusView(info.DriverId, info.PropertyId, WireEnum.ToWire(info.Status), info.RestrictionReason))
            .ToList());
    }
}
