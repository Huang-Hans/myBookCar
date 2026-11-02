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
/// 通知渠道配置接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("通知渠道配置")]
[Route("api/platform/staff/v1/notification-channel-configurations")]
public sealed class NotificationChannelConfigurationsController(
    IConfigurationListHandler<NotificationChannelConfiguration> handler) : ControllerBase
{
    /// <summary>
    /// CF11 分页查询通知渠道配置。
    /// </summary>
    /// <returns>分页的通知渠道配置列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listNotificationChannelConfigurations")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<NotificationChannelView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> ListAsync([FromQuery] NotificationChannelListRequest request, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.ListAsync(
            handler, request.ToListQuery(), ConfigurationControllerSupport.ActiveFilter<NotificationChannelConfiguration>(request.IsActive, item => item.IsActive), ConfigurationViewMapper.ToView, cancellationToken);

    /// <summary>
    /// CF12 查询通知渠道配置详情。
    /// </summary>
    /// <param name="eventType">通知事件类型。</param>
    /// <returns>通知渠道配置详情。</returns>
    // [HttpGet("{eventType}", Name = "getNotificationChannelConfiguration")]
    [NonAction]
    [ProducesResponseType(typeof(ApiEnvelope<NotificationChannelView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> GetAsync(string eventType, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.FindAsync(HttpContext, eventType, handler, ConfigurationViewMapper.ToView, null, cancellationToken);
}
