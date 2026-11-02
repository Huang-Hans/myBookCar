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
/// 通知模板接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("通知模板")]
[Route("api/platform/staff/v1/notification-templates")]
public sealed class NotificationTemplatesController(
    IConfigurationListHandler<NotificationTemplate> handler) : ControllerBase
{
    /// <summary>
    /// CF09 分页查询通知模板。
    /// </summary>
    /// <returns>分页的通知模板列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listNotificationTemplates")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<NotificationTemplateView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> ListAsync([FromQuery] NotificationTemplateListRequest request, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.ListAsync(
            handler, request.ToListQuery(), ConfigurationControllerSupport.ActiveFilter<NotificationTemplate>(request.IsActive, item => item.IsActive), ConfigurationViewMapper.ToView, cancellationToken);

    /// <summary>
    /// CF10 查询通知模板详情。
    /// </summary>
    /// <param name="templateId">通知模板标识。</param>
    /// <returns>通知模板详情。</returns>
    [HttpGet("{templateId}", Name = "getNotificationTemplate")]
    [ProducesResponseType(typeof(ApiEnvelope<NotificationTemplateView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> GetAsync(string templateId, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.FindAsync(HttpContext, templateId, handler, ConfigurationViewMapper.ToView, null, cancellationToken);
}
