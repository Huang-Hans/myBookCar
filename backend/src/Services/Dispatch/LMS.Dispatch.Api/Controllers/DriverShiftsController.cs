using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Shifts;
using LMS.Dispatch.Api.Mapping.Shifts;
using LMS.Dispatch.Application.Shifts.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 按司机查询排班。司机标识由路径传入。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Driver)]
[Tags("排班")]
[Route("api/dispatch/staff/v1/drivers/{driverId}/shifts")]
public sealed class DriverShiftsController(IDriverShiftListQueryHandler queryHandler) : ControllerBase
{
    /// <summary>
    /// SH08 查询一名司机的全部班次。
    /// </summary>
    /// <param name="driverId">司机标识。</param>
    /// <remarks>
    /// 返回该司机的所有班次，不限于未来 15 天。一名司机每个业务日至多一条。
    /// 司机或班次不在调用者物业范围内时，司机按不存在处理。默认按业务日期升序。
    /// search 可匹配物业代码或车牌。响应不缓存。
    /// </remarks>
    /// <returns>分页的班次列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listDriverShifts")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<ShiftView>>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverSelfRead)]
    public async Task<IResult> ListAsync(string driverId, [FromQuery] DriverShiftListRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var principal = HttpContext.GetPrincipal();
        if (!string.Equals(driverId, principal.EntityId, StringComparison.Ordinal))
        {
            return ApiResults.Fail(ApiErrors.InsufficientPermission);
        }

        var list = request.ToListQuery();
        var result = await queryHandler.ListAsync(principal.EntityId, PropertyScope.AllProperties, list, cancellationToken);

        return PagedResponses.ToResult(result, list, ShiftMapper.ToView);
    }
}
