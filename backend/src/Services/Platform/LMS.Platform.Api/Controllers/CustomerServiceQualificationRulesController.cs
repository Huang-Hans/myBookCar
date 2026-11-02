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
/// 客服资格规则接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("客服资格")]
[Route("api/platform/staff/v1/customer-service-qualification-rules")]
public sealed class CustomerServiceQualificationRulesController(IConfigurationDocumentHandler<CsQualificationRules> handler) : ControllerBase
{
    /// <summary>
    /// CF17 查询客服资格规则。
    /// </summary>
    /// <returns>客服资格规则正文；未配置或正文不可读时返回 503 / 50300，不返回演示数据。</returns>
    [HttpGet(Name = "getCustomerServiceQualificationRules")]
    [ProducesResponseType(typeof(ApiEnvelope<CsQualificationRulesView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> GetAsync(CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.GetDocumentAsync(HttpContext, handler, ConfigurationViewMapper.ToView, cancellationToken);
}
