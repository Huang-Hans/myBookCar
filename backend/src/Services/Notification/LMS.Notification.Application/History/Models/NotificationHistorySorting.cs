namespace LMS.Notification.Application.History.Models;

public static class NotificationHistorySorting
{
    public static IReadOnlyList<string> Fields { get; } =
        ["recipient", "channel", "occurredAt", "eventType", "status", "notificationId"];

    public const string DefaultSort = "-occurredAt,notificationId";
    public const string StableField = "notificationId";
}
