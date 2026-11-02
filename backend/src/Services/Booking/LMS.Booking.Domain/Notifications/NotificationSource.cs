namespace LMS.Booking.Domain.Notifications;

// 实时通知事件物化出的站内通知的来源元数据：事件类型字面值、订单物业与来源事件标识。
// 用于客服专用快照筛选和去重；旧通知没有来源，不回填。
public sealed record NotificationSource(string EventType, string PropertyId, string SourceEventId);
