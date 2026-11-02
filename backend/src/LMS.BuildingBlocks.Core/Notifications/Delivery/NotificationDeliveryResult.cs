namespace LMS.BuildingBlocks.Core.Notifications.Delivery;

// Reason 是失败分类或跳过原因（短的大写标识），不含内部细节。
public sealed record NotificationDeliveryResult(NotificationDeliveryOutcome Outcome, string? Reason = null)
{
    public static NotificationDeliveryResult Delivered { get; } = new(NotificationDeliveryOutcome.Delivered);

    public static NotificationDeliveryResult Skipped(string reason) => new(NotificationDeliveryOutcome.Skipped, reason);

    public static NotificationDeliveryResult Retry(string reason) => new(NotificationDeliveryOutcome.Retry, reason);
}
