namespace LMS.BuildingBlocks.Core.Notifications.Delivery;

// 单条事件一次发布尝试的结论。Retry 表示暂时无法判定或发送（例如目录不可用），稍后重试且计入尝试次数。
public enum NotificationDeliveryOutcome
{
    Delivered,
    Skipped,
    Retry
}
