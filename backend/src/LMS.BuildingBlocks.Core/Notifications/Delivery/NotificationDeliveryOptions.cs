namespace LMS.BuildingBlocks.Core.Notifications.Delivery;

// 待发送事件的后台处理参数（技术配置）。具体保留期、间隔与上限由部署决定，默认值只是保守的起点。
public sealed class NotificationDeliveryOptions
{
    public const string SectionName = "Notifications:Delivery";

    // Disable on instances that share the outbox but do not host the recipient connections.
    public bool DispatcherEnabled { get; init; } = true;

    public int PollSeconds { get; init; } = 2;

    public int BatchSize { get; init; } = 50;

    public int LeaseSeconds { get; init; } = 60;

    public int MaxAttempts { get; init; } = 8;

    public int RetryBaseSeconds { get; init; } = 5;

    public int RetryMaxSeconds { get; init; } = 300;

    // 终态事件（已发布 / 跳过 / 失败）保留多少天后才可被清理；未处理的事件没有过期时间。
    public int RetentionDays { get; init; } = 7;

    // 在线连接的重新核验间隔与检查间隔（秒）。
    public int ReverifySeconds { get; init; } = 120;

    public int GuardIntervalSeconds { get; init; } = 15;
}
