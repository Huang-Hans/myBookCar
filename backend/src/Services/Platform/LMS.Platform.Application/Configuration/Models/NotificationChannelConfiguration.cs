namespace LMS.Platform.Application.Configuration.Models;

public sealed record NotificationChannelConfiguration(
    NotificationEventType EventType,
    IReadOnlyList<NotificationChannel> Channels,
    string EventTrigger,
    bool IsActive);
