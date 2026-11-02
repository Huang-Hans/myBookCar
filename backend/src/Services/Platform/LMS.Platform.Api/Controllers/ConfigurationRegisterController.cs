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
/// 配置台账接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("配置台账")]
[Route("api/platform/staff/v1/configuration-register")]
public sealed class ConfigurationRegisterController(
    IConfigurationListHandler<ConfigurationRegisterEntry> handler) : ControllerBase
{
    /// <summary>
    /// CF22 分页查询配置台账。
    /// </summary>
    /// <returns>分页的配置台账列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listConfigurationRegister")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<ConfigurationRegisterView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> ListAsync([FromQuery] ConfigurationRegisterListRequest request, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.ListAsync(handler, request.ToListQuery(), null, ConfigurationViewMapper.ToView, cancellationToken);
}
