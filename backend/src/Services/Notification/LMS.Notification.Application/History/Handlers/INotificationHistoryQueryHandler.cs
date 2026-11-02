using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Notification.Application.History.Models;
using LMS.Notification.Domain.History;

namespace LMS.Notification.Application.History.Handlers;

public interface INotificationHistoryQueryHandler
{
    Task<OperationResult<PageResult<NotificationHistoryEntry>>> ListAsync(
        ListNotificationHistoryQuery query,
        PropertyScope scope,
        CancellationToken cancellationToken);

    // 内容预览只读取已记录的预览文本，不触发任何发送。
    Task<OperationResult<NotificationHistoryEntry>> GetAsync(string notificationId, PropertyScope scope, CancellationToken cancellationToken);
}
