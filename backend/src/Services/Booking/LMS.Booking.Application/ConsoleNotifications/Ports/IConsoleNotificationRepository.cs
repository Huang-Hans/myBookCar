using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.ConsoleNotifications.Models;
using LMS.Booking.Domain.Notifications;

namespace LMS.Booking.Application.ConsoleNotifications.Ports;

public interface IConsoleNotificationRepository
{
    Task<PageResult<ConsoleNotification>> ListAsync(ListConsoleNotificationsQuery query, CancellationToken cancellationToken);

    Task<long> CountUnreadAsync(string recipientUserId, CancellationToken cancellationToken);

    Task<bool> TryMarkReadAsync(string notificationId, string recipientUserId, DateTimeOffset readAt, CancellationToken cancellationToken);

    Task<long> MarkAllReadAsync(string recipientUserId, DateTimeOffset readAt, CancellationToken cancellationToken);

    Task InsertAsync(ConsoleNotification notification, ITransactionContext context, CancellationToken cancellationToken);
}
