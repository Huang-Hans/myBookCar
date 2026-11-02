namespace LMS.Notification.Api.Contracts.History;

/// <summary>
/// 通知投递历史视图，映射 notification_history。
/// </summary>
/// <param name="NotificationId">通知记录标识（Log ID）。</param>
/// <param name="Recipient">接收人的显示标签；不含额外地址或设备令牌。</param>
/// <param name="Channel">投递渠道：EMAIL / SMS / PUSH / IN_APP。</param>
/// <param name="OccurredAt">投递时间（UTC）。</param>
/// <param name="EventType">触发事件：BOOKING_CONFIRMATION / TRIP_REMINDER / DRIVER_ASSIGNED / TRIP_STARTED / BOOKING_CANCELLED。</param>
/// <param name="Status">投递状态：SENT / DELIVERED / FAILED。</param>
/// <param name="ContentPreview">内容预览；不含完整证件、令牌或供应商的私密错误。</param>
public sealed record NotificationHistoryView(
    string NotificationId,
    string Recipient,
    string Channel,
    DateTime OccurredAt,
    string EventType,
    string Status,
    string ContentPreview);
