using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Domain.Notifications;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class ConsoleNotificationDocument
{
    [BsonId]
    public string NotificationId { get; init; } = string.Empty;

    [BsonElement("recipientUserId")]
    public string RecipientUserId { get; init; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; init; } = string.Empty;

    [BsonElement("summary")]
    public string Summary { get; init; } = string.Empty;

    [BsonElement("tripReference")]
    public string? TripReference { get; init; }

    [BsonElement("occurredAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAt { get; init; }

    // TTL 字段：写入时按保留天数计算；索引 ttl_expires_at 依赖它清理。
    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; init; }

    // 点击后的跳转目标；存量通知没有这两个元素。
    [BsonElement("targetType")]
    public string? TargetType { get; init; }

    [BsonElement("targetId")]
    public string? TargetId { get; init; }

    // 实时事件物化的来源元数据（事件类型、订单物业、来源事件标识）；存量通知没有这三个元素。
    [BsonElement("eventType")]
    public string? EventType { get; init; }

    [BsonElement("propertyId")]
    public string? PropertyId { get; init; }

    [BsonElement("sourceEventId")]
    public string? SourceEventId { get; init; }

    [BsonElement("isRead")]
    public bool IsRead { get; init; }

    [BsonElement("readAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ReadAt { get; init; }

    public static ConsoleNotificationDocument FromDomain(ConsoleNotification notification) => new()
    {
        NotificationId = notification.NotificationId,
        RecipientUserId = notification.RecipientUserId,
        Type = WireEnum.ToWire(notification.Type),
        Summary = notification.Summary,
        TripReference = notification.TripReference,
        OccurredAt = UtcDateTimes.ToStored(notification.OccurredAt),
        ExpiresAt = UtcDateTimes.ToStored(notification.ExpiresAt),
        TargetType = notification.Target?.Type,
        TargetId = notification.Target?.Id,
        EventType = notification.Source?.EventType,
        PropertyId = notification.Source?.PropertyId,
        SourceEventId = notification.Source?.SourceEventId,
        IsRead = notification.IsRead,
        ReadAt = notification.ReadAt is null ? null : UtcDateTimes.ToStored(notification.ReadAt.Value)
    };

    public ConsoleNotification ToDomain() => new(
        NotificationId,
        RecipientUserId,
        StoredValues.ParseEnum<NotificationType>(Type, "type"),
        Summary,
        TripReference,
        UtcDateTimes.FromStored(OccurredAt),
        UtcDateTimes.FromStored(ExpiresAt),
        IsRead,
        ReadAt is null ? null : UtcDateTimes.FromStored(ReadAt.Value),
        TargetType is null || TargetId is null ? null : new NotificationTarget(TargetType, TargetId),
        EventType is null || PropertyId is null || SourceEventId is null ? null : new NotificationSource(EventType, PropertyId, SourceEventId));
}
