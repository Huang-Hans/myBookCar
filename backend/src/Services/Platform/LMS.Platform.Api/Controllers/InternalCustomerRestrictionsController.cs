using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Internal;
using LMS.Platform.Application.CustomerBlacklists.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 服务间接口：核对客户当前是否受限制（客户黑名单的有效性判断）。
/// </summary>
/// <remarks>
/// 不经网关；Booking 在审批前核对与黑名单命中查询时调用，转发终端用户的令牌，并重新检查权限。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1/customer-restriction-checks")]
public sealed class InternalCustomerRestrictionsController(ICheckCustomerRestrictionsHandler restrictionsHandler) : ControllerBase
{
    private const int MaxMemberIds = 500;

    /// <summary>
    /// 批量核对客户限制。
    /// </summary>
    /// <returns>命中的客户限制列表；没有命中时为空数组。</returns>
    [HttpPost]
    [RequirePermission(PermissionCodes.ApprovalsRead)]
    public async Task<IResult> CheckAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CustomerRestrictionCheckRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var memberIds = body.Value.MemberIds;
        if (memberIds is null || memberIds.Count > MaxMemberIds || memberIds.Any(string.IsNullOrWhiteSpace))
        {
            return ApiResults.ValidationFailed([new ApiFieldError("memberIds", $"Must contain 0 to {MaxMemberIds} non-blank member ids")]);
        }

        var hits = await restrictionsHandler.HandleAsync(memberIds.Select(id => id!.Trim()).ToList(), cancellationToken);

        return ApiResults.Ok(hits.Select(hit => new CustomerRestrictionView(hit.MemberId, hit.Reason)).ToList());
    }
}
