namespace LMS.Booking.Domain.Notifications;

// 站内通知（收件人 = 门户用户）。summary 为可展示英文短语，不含内部细节；过期由 expiresAt TTL 清理。
public sealed record ConsoleNotification(
    string NotificationId,
    string RecipientUserId,
    NotificationType Type,
    string Summary,
    string? TripReference,
    DateTimeOffset OccurredAt,
    DateTimeOffset ExpiresAt,
    bool IsRead,
    DateTimeOffset? ReadAt,
    NotificationTarget? Target = null,
    NotificationSource? Source = null)
{
    public const string IdPrefix = "notice_";

    public static ConsoleNotification Create(
        string recipientUserId,
        NotificationType type,
        string summary,
        string? tripReference,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        NotificationTarget? target = null) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            recipientUserId, type, summary, tripReference, now, expiresAt, false, null, target);

    // 实时事件物化的站内通知：标识由事件稳定推导，重试得到同一条；类型固定为 SYSTEM_ALERT。
    public static ConsoleNotification Materialized(
        string notificationId,
        string recipientUserId,
        string summary,
        string? tripReference,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        NotificationTarget target,
        NotificationSource source) =>
        new(notificationId, recipientUserId, NotificationType.SystemAlert, summary, tripReference, now, expiresAt, false, null, target, source);

    public ConsoleNotification AsRead(DateTimeOffset readAt) => this with { IsRead = true, ReadAt = readAt };
}
