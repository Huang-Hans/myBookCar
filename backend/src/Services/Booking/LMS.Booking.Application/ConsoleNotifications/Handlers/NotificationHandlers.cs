using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.ConsoleNotifications.Models;
using LMS.Booking.Domain.Notifications;

namespace LMS.Booking.Application.ConsoleNotifications.Handlers;

public interface IConsoleNotificationQueryHandler
{
    Task<OperationResult<NotificationPage>> ListAsync(ListConsoleNotificationsQuery query, CancellationToken cancellationToken);

    Task<long> CountUnreadAsync(string recipientUserId, CancellationToken cancellationToken);
}

public interface IMarkConsoleNotificationReadHandler
{
    Task<OperationResult<bool>> MarkReadAsync(string notificationId, string recipientUserId, CancellationToken cancellationToken);
}

public interface IMarkAllConsoleNotificationsReadHandler
{
    Task<long> MarkAllReadAsync(string recipientUserId, CancellationToken cancellationToken);
}

// 通知分页 + 未读计数（HA-11 列表页）。
public sealed record NotificationPage(PageResult<ConsoleNotification> Page, long UnreadCount);
