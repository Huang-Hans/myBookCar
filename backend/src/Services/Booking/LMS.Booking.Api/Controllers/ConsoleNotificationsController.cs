using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.ConsoleNotifications;
using LMS.Booking.Application.ConsoleNotifications.Handlers;
using LMS.Booking.Application.ConsoleNotifications.Models;
using LMS.Booking.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 站内通知中心（HA-11）：列表按收件人（本人）过滤；标记已读仅限本人通知。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("客服通知")]
public sealed class ConsoleNotificationsController(
    IConsoleNotificationQueryHandler queryHandler,
    IMarkConsoleNotificationReadHandler markReadHandler,
    IMarkAllConsoleNotificationsReadHandler markAllHandler) : ControllerBase
{
    private static readonly ListQuerySpec ListSpec =
        new(ConsoleNotificationSorting.Fields, ConsoleNotificationSorting.DefaultSort, ConsoleNotificationSorting.StableField);

    /// <summary>
    /// 分页查询当前用户的通知。
    /// </summary>
    /// <remarks>
    /// 查询参数：page、pageSize、sort、search，以及 type、unread。
    /// </remarks>
    [HttpGet("api/booking/staff/v1/console-notifications", Name = "listConsoleNotifications")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<ConsoleNotificationView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.NotificationsRead)]
    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        var reader = QueryReader.For(HttpContext);
        var list = reader.ReadList(ListSpec);
        var type = reader.OptionalEnum<NotificationType>("type");
        var unread = reader.OptionalBool("unread");
        if (reader.Failure() is { } failure)
        {
            return failure;
        }

        var query = new ListConsoleNotificationsQuery(list, UserId, type, unread == true);
        var result = await queryHandler.ListAsync(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return OperationResultMapper.ToFailure(result);
        }

        return PagedResponses.ToResult(result.Value!.Page, list, ToView);
    }

    /// <summary>当前用户未读通知数（列表页角标）。</summary>
    [HttpGet("api/booking/staff/v1/console-notifications/unread-count", Name = "countUnreadConsoleNotifications")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.NotificationsRead)]
    public async Task<IResult> CountUnreadAsync(CancellationToken cancellationToken)
    {
        var unread = await queryHandler.CountUnreadAsync(UserId, cancellationToken);

        return ApiResults.Ok(new { unreadCount = unread });
    }

    /// <summary>标记单条已读（幂等）。</summary>
    [HttpPost("api/booking/staff/v1/console-notifications/{notificationId}/read", Name = "markConsoleNotificationRead")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.NotificationsRead)]
    public async Task<IResult> MarkReadAsync(string notificationId, CancellationToken cancellationToken)
    {
        var result = await markReadHandler.MarkReadAsync(notificationId, UserId, cancellationToken);

        return result.IsSuccess
            ? ApiResults.Ok(new { marked = true })
            : OperationResultMapper.ToFailure(result);
    }

    /// <summary>全部标记已读，返回标记条数。</summary>
    [HttpPost("api/booking/staff/v1/console-notifications/read-all", Name = "markAllConsoleNotificationsRead")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.NotificationsRead)]
    public async Task<IResult> MarkAllReadAsync(CancellationToken cancellationToken)
    {
        var marked = await markAllHandler.MarkAllReadAsync(UserId, cancellationToken);

        return ApiResults.Ok(new { markedCount = marked });
    }

    private string UserId => HttpContext.GetCaller().UserId;

    private static ConsoleNotificationView ToView(ConsoleNotification notification) => new(
        notification.NotificationId,
        WireEnum.ToWire(notification.Type),
        notification.Summary,
        notification.TripReference,
        notification.OccurredAt,
        notification.IsRead,
        notification.ReadAt,
        notification.Target?.Type,
        notification.Target?.Id);
}
