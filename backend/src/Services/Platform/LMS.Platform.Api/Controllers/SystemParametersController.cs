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
/// 系统参数接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("系统参数")]
[Route("api/platform/staff/v1/system-parameters")]
public sealed class SystemParametersController(IConfigurationDocumentHandler<SystemParameters> handler) : ControllerBase
{
    /// <summary>
    /// CF19 查询系统参数。
    /// </summary>
    /// <returns>系统参数正文；未配置或正文不可读时返回 503 / 50300，不返回演示数据。</returns>
    [HttpGet(Name = "getSystemParameters")]
    [ProducesResponseType(typeof(ApiEnvelope<SystemParametersView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> GetAsync(CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.GetDocumentAsync(HttpContext, handler, ConfigurationViewMapper.ToView, cancellationToken);
}
