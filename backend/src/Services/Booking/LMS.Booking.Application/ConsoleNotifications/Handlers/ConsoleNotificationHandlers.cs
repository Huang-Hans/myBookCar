using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.ConsoleNotifications.Models;
using LMS.Booking.Application.ConsoleNotifications.Ports;
using LMS.Booking.Domain.Notifications;

namespace LMS.Booking.Application.ConsoleNotifications.Handlers;

// 通知中心（HA-11）：列表按收件人过滤；已读标记按收件人限定，不越权操作他人通知。
internal sealed class ConsoleNotificationQueryHandler(
    IConsoleNotificationRepository notifications) : IConsoleNotificationQueryHandler
{
    public async Task<OperationResult<NotificationPage>> ListAsync(ListConsoleNotificationsQuery query, CancellationToken cancellationToken)
    {
        var page = await notifications.ListAsync(query, cancellationToken);
        var unread = await notifications.CountUnreadAsync(query.RecipientUserId, cancellationToken);

        return OperationResult<NotificationPage>.Succeeded(new NotificationPage(page, unread));
    }

    public Task<long> CountUnreadAsync(string recipientUserId, CancellationToken cancellationToken) =>
        notifications.CountUnreadAsync(recipientUserId, cancellationToken);
}

internal sealed class MarkConsoleNotificationReadHandler(
    IConsoleNotificationRepository notifications,
    TimeProvider timeProvider) : IMarkConsoleNotificationReadHandler
{
    public async Task<OperationResult<bool>> MarkReadAsync(string notificationId, string recipientUserId, CancellationToken cancellationToken)
    {
        // 已读标记幂等：重复标记同样返回成功。
        var marked = await notifications.TryMarkReadAsync(notificationId, recipientUserId, timeProvider.GetUtcNow(), cancellationToken);

        return marked
            ? OperationResult<bool>.Succeeded(true)
            : OperationResult<bool>.Failed(OperationOutcome.NotFound);
    }
}

internal sealed class MarkAllConsoleNotificationsReadHandler(
    IConsoleNotificationRepository notifications,
    TimeProvider timeProvider) : IMarkAllConsoleNotificationsReadHandler
{
    public Task<long> MarkAllReadAsync(string recipientUserId, CancellationToken cancellationToken) =>
        notifications.MarkAllReadAsync(recipientUserId, timeProvider.GetUtcNow(), cancellationToken);
}
