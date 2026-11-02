using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.ConsoleNotifications;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.Staff;
using LMS.Booking.Application.Staff.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 员工控制台偏好（Console Settings / 通知偏好 / 地点收藏）：全部本人自助，按令牌主体定位，不接受 userId 字段。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("客服员工偏好")]
public sealed class StaffController(
    IStaffPreferencesQueryHandler queryHandler,
    IStaffConsoleSettingsUpdateHandler settingsHandler,
    IStaffNotificationPreferencesUpdateHandler notificationPreferencesHandler,
    IStaffLocationFavoritesUpdateHandler favoritesHandler) : ControllerBase
{
    /// <summary>读取本人偏好（未设置时返回默认值）。</summary>
    [HttpGet("api/booking/staff/v1/console-settings", Name = "getStaffConsoleSettings")]
    [RequireCaller]
    [ProducesResponseType(typeof(ApiEnvelope<StaffPreferencesView>), StatusCodes.Status200OK)]
    public async Task<IResult> GetAsync(CancellationToken cancellationToken)
    {
        var preferences = await queryHandler.GetAsync(HttpContext.GetCaller().UserId, cancellationToken);

        return ApiResults.Ok(StaffMapper.ToView(preferences));
    }

    /// <summary>更新工作站设定（站点、默认通知渠道、宾客确认语言）。</summary>
    [HttpPut("api/booking/staff/v1/console-settings", Name = "updateStaffConsoleSettings")]
    [ApiRequestBody(typeof(ConsoleSettingsRequest))]
    [RequireCaller]
    [ProducesResponseType(typeof(ApiEnvelope<StaffPreferencesView>), StatusCodes.Status200OK)]
    public async Task<IResult> UpdateSettingsAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<ConsoleSettingsRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var settings = StaffMapper.ToSettings(body.Value);
        if (settings.Failure is not null)
        {
            return settings.Failure;
        }

        var result = await settingsHandler.UpdateSettingsAsync(
            HttpContext.GetCaller().UserId,
            settings.Value.Station,
            settings.Value.Channel,
            settings.Value.Language,
            CallerContexts.From(HttpContext),
            cancellationToken);

        return OperationResultMapper.ToResult(result, StaffMapper.ToView);
    }

    /// <summary>更新通知偏好（6 类事件开关）。</summary>
    [HttpPut("api/booking/staff/v1/notification-preferences", Name = "updateStaffNotificationPreferences")]
    [ApiRequestBody(typeof(NotificationPreferencesRequest))]
    [RequireCaller]
    [ProducesResponseType(typeof(ApiEnvelope<StaffPreferencesView>), StatusCodes.Status200OK)]
    public async Task<IResult> UpdateNotificationPreferencesAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<NotificationPreferencesRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var types = StaffMapper.ToNotificationTypes(body.Value);
        if (types.Failure is not null)
        {
            return types.Failure;
        }

        var result = await notificationPreferencesHandler.UpdateNotificationPreferencesAsync(
            HttpContext.GetCaller().UserId, types.Value, CallerContexts.From(HttpContext), cancellationToken);

        return OperationResultMapper.ToResult(result, StaffMapper.ToView);
    }

    /// <summary>更新地点收藏（全量替换；地点必须存在且启用）。</summary>
    [HttpPut("api/booking/staff/v1/location-favorites", Name = "updateStaffLocationFavorites")]
    [ApiRequestBody(typeof(LocationFavoritesRequest))]
    [RequireCaller]
    [ProducesResponseType(typeof(ApiEnvelope<StaffPreferencesView>), StatusCodes.Status200OK)]
    public async Task<IResult> UpdateLocationFavoritesAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<LocationFavoritesRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var ids = StaffMapper.ToLocationIds(body.Value);
        if (ids.Failure is not null)
        {
            return ids.Failure;
        }

        var result = await favoritesHandler.UpdateFavoritesAsync(
            HttpContext.GetCaller().UserId, ids.Value, CallerContexts.From(HttpContext), cancellationToken);

        return OperationResultMapper.ToResult(result, StaffMapper.ToView);
    }

    /// <summary>读取地点收藏与最近使用（HA-07 快捷选择）。</summary>
    [HttpGet("api/booking/staff/v1/location-favorites", Name = "getStaffLocationFavorites")]
    [RequireCaller]
    [ProducesResponseType(typeof(ApiEnvelope<StaffPreferencesView>), StatusCodes.Status200OK)]
    public async Task<IResult> GetLocationFavoritesAsync(CancellationToken cancellationToken)
    {
        var preferences = await queryHandler.GetAsync(HttpContext.GetCaller().UserId, cancellationToken);

        return ApiResults.Ok(StaffMapper.ToView(preferences));
    }
}
