using LMS.BuildingBlocks.Core.Notifications.Events;

namespace LMS.BuildingBlocks.Core.Notifications.Delivery;

// 待发送事件的后台处理端口：条件领取（带期限租约）与按租约持有者条件地写回结果。
// 写回返回 false 表示租约已被他人接管，调用方放弃本次结果。
public interface INotificationOutbox
{
    Task<IReadOnlyList<NotificationEvent>> ClaimDueAsync(string leaseOwner, DateTimeOffset now, TimeSpan lease, int take, CancellationToken cancellationToken);

    Task<bool> MarkPublishedAsync(string eventId, string leaseOwner, DateTimeOffset now, DateTimeOffset purgeAt, CancellationToken cancellationToken);

    Task<bool> MarkSkippedAsync(string eventId, string leaseOwner, string reason, DateTimeOffset purgeAt, CancellationToken cancellationToken);

    Task<bool> MarkFailedAsync(string eventId, string leaseOwner, string reason, DateTimeOffset purgeAt, CancellationToken cancellationToken);

    Task<bool> RescheduleAsync(string eventId, string leaseOwner, int attempts, DateTimeOffset nextAttemptAt, string reason, CancellationToken cancellationToken);
}
