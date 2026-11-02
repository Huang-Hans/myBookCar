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
/// 自助预订规则与自助资格说明接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("自助预订")]
[Route("api/platform/staff/v1")]
public sealed class SelfBookingRulesController(
    IConfigurationDocumentHandler<SelfBookingRules> bookingRulesHandler,
    IConfigurationDocumentHandler<EligibilityRules> eligibilityRulesHandler) : ControllerBase
{
    /// <summary>
    /// CF15 查询自助预约规则。
    /// </summary>
    /// <returns>自助预约规则正文；未配置或正文不可读时返回 503 / 50300，不返回演示数据。</returns>
    [HttpGet("self-booking-rules", Name = "getSelfBookingRules")]
    [ProducesResponseType(typeof(ApiEnvelope<SelfBookingRulesView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> GetBookingRulesAsync(CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.GetDocumentAsync(HttpContext, bookingRulesHandler, ConfigurationViewMapper.ToView, cancellationToken);

    /// <summary>
    /// CF16 查询自助资格说明。
    /// </summary>
    /// <returns>自助资格说明正文；未配置或正文不可读时返回 503 / 50300，不返回演示数据。</returns>
    [HttpGet("self-booking-eligibility-rules", Name = "getSelfBookingEligibilityRules")]
    [ProducesResponseType(typeof(ApiEnvelope<EligibilityRulesView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> GetEligibilityRulesAsync(CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.GetDocumentAsync(HttpContext, eligibilityRulesHandler, ConfigurationViewMapper.ToView, cancellationToken);
}
