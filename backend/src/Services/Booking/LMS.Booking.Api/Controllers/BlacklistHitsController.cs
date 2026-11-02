using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.Approvals;
using LMS.Booking.Api.Mapping.Approvals;
using LMS.Booking.Application.Approvals.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 黑名单命中接口。
/// </summary>
/// <remarks>
/// 命中数据没有独立存储：由 Booking 的待审核队列实时结合 Platform 的客户限制与 Dispatch 的司机状态生成，
/// 因此服务归属为 Booking，而不是维护黑名单主档的 Platform。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("黑名单命中")]
[Route("api/booking/staff/v1/blacklist-hits")]
public sealed class BlacklistHitsController(IBlacklistHitQueryHandler handler) : ControllerBase
{
    /// <summary>
    /// BL05 查询黑名单命中。
    /// </summary>
    /// <remarks>
    /// 命中列表不分页。
    /// </remarks>
    /// <returns>黑名单命中列表；列表不分页，因此没有 meta.pagination。Platform 不可用时返回 503 / 50300。</returns>
    [HttpGet(Name = "listBlacklistHits")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<BlacklistHitView>>), StatusCodes.Status200OK)]
    // 授权要求待确认：当前沿用 approvals:read（命中含审批与行程信息，且下游内部接口同样要求 approvals:read）。
    [RequirePermission(PermissionCodes.ApprovalsRead)]
    public async Task<IResult> ListAsync([FromQuery] BlacklistHitListRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(HttpContext.GetCaller().Scope, request.PropertyId, cancellationToken);

        return result.IsSuccess
            ? ApiResults.Ok(result.Value!.Select(ApprovalViewMapper.ToView).ToList())
            : OperationResultMapper.ToFailure(result);
    }
}
