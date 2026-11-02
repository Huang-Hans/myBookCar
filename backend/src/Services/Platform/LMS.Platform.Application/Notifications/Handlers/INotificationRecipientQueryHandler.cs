using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Notifications.Models;

namespace LMS.Platform.Application.Notifications.Handlers;

public interface INotificationRecipientQueryHandler
{
    // 成功时返回有资格接收该物业该事件的员工本地 userId（可为空集合）；目录未配置或超过上限为 SourceUnavailable。
    Task<OperationResult<IReadOnlyList<string>>> HandleAsync(NotificationRecipientQuery query, CancellationToken cancellationToken);
}
