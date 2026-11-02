namespace LMS.Notification.Domain.History;

// 一条真实的通知投递记录。Recipient 只是显示标签，不保存额外地址或设备令牌；
// ContentPreview 不得包含完整证件、令牌或供应商的私密错误。投递事实由外部来源写入，本服务只读。
public sealed record NotificationHistoryEntry(
    string NotificationId,
    string Recipient,
    NotificationChannel Channel,
    DateTimeOffset OccurredAt,
    NotificationEventType EventType,
    NotificationStatus Status,
    string ContentPreview);
