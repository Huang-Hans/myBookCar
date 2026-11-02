using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Domain.Notifications;

namespace LMS.Booking.Application.ConsoleNotifications.Models;

public sealed record ListConsoleNotificationsQuery(ListQuery List, string RecipientUserId, NotificationType? Type, bool UnreadOnly);

public static class ConsoleNotificationSorting
{
    public static IReadOnlyList<string> Fields { get; } = ["occurredAt", "type", "summary", "tripReference", "notificationId"];

    public const string DefaultSort = "-occurredAt,notificationId";
    public const string StableField = "notificationId";
}
