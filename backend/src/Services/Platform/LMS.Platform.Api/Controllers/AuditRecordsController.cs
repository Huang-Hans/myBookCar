using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Audit;
using LMS.Platform.Api.Mapping.Audit;
using LMS.Platform.Application.AuditRecords.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 审计记录接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("审计记录")]
[Route("api/platform/staff/v1/audit-records")]
public sealed class AuditRecordsController(IAuditRecordQueryHandler queryHandler) : ControllerBase
{
    /// <summary>
    /// AU01 分页查询审计记录。
    /// </summary>
    /// <returns>分页的审计记录列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listAuditRecords")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<AuditView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.AuditRead)]
    public async Task<IResult> ListAsync([FromQuery] AuditRecordListRequest request, CancellationToken cancellationToken)
    {
        var query = AuditRequestMapper.ToListQuery(request, HttpContext.GetCaller().Scope);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, AuditViewMapper.ToView);
    }
}
