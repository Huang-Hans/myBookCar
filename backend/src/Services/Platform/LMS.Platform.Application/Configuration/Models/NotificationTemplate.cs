namespace LMS.Platform.Application.Configuration.Models;

public sealed record NotificationTemplate(
    string TemplateId,
    NotificationEventType EventType,
    string EventTrigger,
    IReadOnlyList<NotificationChannel> Channels,
    string ContentTemplate,
    bool IsActive);
