namespace LMS.BuildingBlocks.Core.Notifications.Events;

// 已登记的待发送事件及其处理进度。NotificationId 只有客服的站内通知有值。
public sealed record NotificationEvent(
    NotificationEventDraft Draft,
    string? NotificationId,
    NotificationEventState State,
    int Attempts,
    DateTimeOffset NextAttemptAt)
{
    public string EventId => Draft.EventId;
}
