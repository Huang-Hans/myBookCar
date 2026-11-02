namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 通知渠道配置视图，按事件类型给出启用的通知渠道。
/// </summary>
/// <param name="EventType">事件类型，BOOKING_CONFIRMATION / TRIP_REMINDER / DRIVER_ASSIGNED / TRIP_STARTED / BOOKING_CANCELLED。</param>
/// <param name="Channels">该事件启用的渠道，EMAIL / SMS / PUSH / IN_APP；非空且去重。</param>
/// <param name="EventTrigger">事件触发条件标签。</param>
/// <param name="IsActive">配置是否启用。</param>
public sealed record NotificationChannelView(
    string EventType,
    IReadOnlyList<string> Channels,
    string EventTrigger,
    bool IsActive);
