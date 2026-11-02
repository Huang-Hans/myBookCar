namespace LMS.BuildingBlocks.Core.Notifications.Contracts;

// 客服通知快照中的一条下单通知。notification.notificationId 非空。
public sealed record NotificationSnapshotItemV1(
    string EventId,
    string EventType,
    NotificationContentV1 Notification,
    bool IsRead,
    DateTime? ReadAt);
