using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
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
/// 会员等级与资格来源说明接口。
/// </summary>
/// <remarks>
/// 资格来源说明解释会员等级如何取得资格，与会员等级归为同一资源族。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("会员等级")]
[Route("api/platform/staff/v1")]
public sealed class MembershipTiersController(
    IConfigurationListHandler<MembershipTier> tierHandler,
    IConfigurationDocumentHandler<LoyaltyQualificationRules> qualificationRulesHandler) : ControllerBase
{
    /// <summary>
    /// CF13 查询会员等级。
    /// </summary>
    /// <remarks>
    /// 等级卡片与矩阵不分页；有效会员数来自外部会员参考（Q03），未接入时省略。
    /// </remarks>
    /// <returns>会员等级列表；列表不分页，因此没有 meta.pagination。</returns>
    [HttpGet("membership-tiers", Name = "listMembershipTiers")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<MembershipTierView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        return await ConfigurationControllerSupport.ListAllAsync(tierHandler, null, ConfigurationViewMapper.ToView, cancellationToken);
    }

    /// <summary>
    /// CF14 查询资格来源说明。
    /// </summary>
    /// <returns>资格来源说明正文；未配置或正文不可读时返回 503 / 50300，不返回演示数据。</returns>
    [HttpGet("loyalty-qualification-rules", Name = "getLoyaltyQualificationRules")]
    [ProducesResponseType(typeof(ApiEnvelope<LoyaltyQualificationRulesView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> GetQualificationRulesAsync(CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.GetDocumentAsync(HttpContext, qualificationRulesHandler, ConfigurationViewMapper.ToView, cancellationToken);
}
