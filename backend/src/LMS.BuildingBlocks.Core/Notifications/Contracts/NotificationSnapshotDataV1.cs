namespace LMS.BuildingBlocks.Core.Notifications.Contracts;

// 通知快照的业务数据。unreadCount 是当前可见下单通知的总未读数，不是当前页条数。
public sealed record NotificationSnapshotDataV1(IReadOnlyList<NotificationSnapshotItemV1> Items, long UnreadCount);
