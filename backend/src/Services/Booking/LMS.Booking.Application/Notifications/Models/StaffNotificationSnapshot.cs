using LMS.Booking.Domain.Notifications;

namespace LMS.Booking.Application.Notifications.Models;

// Items 是当前页；TotalItems 是同一范围内的下单通知总数；UnreadCount 是同一范围内的总未读数（不是当前页数量）。
public sealed record StaffNotificationSnapshot(IReadOnlyList<ConsoleNotification> Items, long TotalItems, long UnreadCount);
