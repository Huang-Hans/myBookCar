namespace LMS.BuildingBlocks.Core.Notifications.Contracts;

// 事件容器。eventId 是接收人的稳定事件标识，重试发布保持不变，客户端据此去重；schemaVersion 固定 1。
public sealed record NotificationEventDataV1(
    string EventId,
    int SchemaVersion,
    string EventType,
    NotificationContentV1 Notification);
