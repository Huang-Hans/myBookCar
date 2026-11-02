using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Configuration;
using LMS.Platform.Api.Controllers.Support;
using LMS.Platform.Api.Mapping.Configuration;
using LMS.Platform.Application.Configuration.Handlers;
using LMS.Platform.Application.Configuration.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 取消原因接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("取消原因")]
[Route("api/platform/staff/v1/cancellation-reasons")]
public sealed class CancellationReasonsController(
    IConfigurationListHandler<CancellationReason> handler) : ControllerBase
{
    /// <summary>
    /// CF03 分页查询取消原因。
    /// </summary>
    /// <returns>分页的取消原因列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listCancellationReasons")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CancellationReasonView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> ListAsync([FromQuery] CancellationReasonListRequest request, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.ListAsync(handler, request.ToListQuery(), null, ConfigurationViewMapper.ToView, cancellationToken);

    /// <summary>
    /// CF04 查询取消原因详情。
    /// </summary>
    /// <param name="reasonCode">取消原因代码。</param>
    /// <returns>取消原因详情。</returns>
    [HttpGet("{reasonCode}", Name = "getCancellationReason")]
    [ProducesResponseType(typeof(ApiEnvelope<CancellationReasonView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> GetAsync(string reasonCode, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.FindAsync(HttpContext, reasonCode, handler, ConfigurationViewMapper.ToView, null, cancellationToken);
}
