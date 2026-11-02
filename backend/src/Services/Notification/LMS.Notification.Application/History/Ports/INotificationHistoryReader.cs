using LMS.BuildingBlocks.Core.Querying;
using LMS.Notification.Application.History.Models;
using LMS.Notification.Domain.History;

namespace LMS.Notification.Application.History.Ports;

public interface INotificationHistoryReader
{
    Task<PageResult<NotificationHistoryEntry>> ListAsync(ListNotificationHistoryQuery query, CancellationToken cancellationToken);

    Task<NotificationHistoryEntry?> FindAsync(string notificationId, CancellationToken cancellationToken);
}
