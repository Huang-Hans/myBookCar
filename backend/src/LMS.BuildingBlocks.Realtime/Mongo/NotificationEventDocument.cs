using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.BuildingBlocks.Core.Wire;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.BuildingBlocks.Realtime.Mongo;

// 只保存发布所需的信息：不保存 Bearer 令牌、完整订单或任何供应商凭据。
internal sealed class NotificationEventDocument
{
    [BsonId]
    public string EventId { get; init; } = string.Empty;

    [BsonElement("eventType")]
    public string EventType { get; init; } = string.Empty;

    [BsonElement("recipientKind")]
    public string RecipientKind { get; init; } = string.Empty;

    // 为空表示按 propertyId 扇出给物业范围内的授权员工。
    [BsonElement("recipientId")]
    public string? RecipientId { get; init; }

    [BsonElement("propertyId")]
    public string? PropertyId { get; init; }

    [BsonElement("notificationId")]
    public string? NotificationId { get; init; }

    [BsonElement("summary")]
    public string Summary { get; init; } = string.Empty;

    [BsonElement("tripReference")]
    public string? TripReference { get; init; }

    [BsonElement("targetType")]
    public string TargetType { get; init; } = string.Empty;

    [BsonElement("targetId")]
    public string TargetId { get; init; } = string.Empty;

    [BsonElement("traceId")]
    public string TraceId { get; init; } = string.Empty;

    [BsonElement("occurredAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAt { get; init; }

    [BsonElement("validUntil")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ValidUntil { get; init; }

    [BsonElement("state")]
    public string State { get; init; } = string.Empty;

    [BsonElement("attempts")]
    public int Attempts { get; init; }

    [BsonElement("nextAttemptAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime NextAttemptAt { get; init; }

    [BsonElement("leaseOwner")]
    public string? LeaseOwner { get; init; }

    [BsonElement("leaseUntil")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LeaseUntil { get; init; }

    [BsonElement("failureCategory")]
    public string? FailureCategory { get; init; }

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; }

    [BsonElement("publishedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? PublishedAt { get; init; }

    // 仅终态事件写入；TTL 索引据此清理。
    [BsonElement("purgeAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? PurgeAt { get; init; }

    public static NotificationEventDocument FromDraft(NotificationEventDraft draft, string? notificationId, DateTimeOffset now) => new()
    {
        EventId = draft.EventId,
        EventType = WireEnum.ToWire(draft.EventType),
        RecipientKind = WireEnum.ToWire(draft.RecipientKind),
        RecipientId = draft.RecipientId,
        PropertyId = draft.PropertyId,
        NotificationId = notificationId,
        Summary = draft.Summary,
        TripReference = draft.TripReference,
        TargetType = draft.TargetType,
        TargetId = draft.TargetId,
        TraceId = draft.TraceId,
        OccurredAt = draft.OccurredAt.UtcDateTime,
        ValidUntil = draft.ValidUntil?.UtcDateTime,
        State = WireEnum.ToWire(NotificationEventState.Pending),
        Attempts = 0,
        NextAttemptAt = now.UtcDateTime,
        CreatedAt = now.UtcDateTime
    };

    public NotificationEvent ToDomain() => new(
        new NotificationEventDraft(
            EventId,
            Parse<RealtimeEventType>(EventType, "eventType"),
            Parse<RealtimeRecipientKind>(RecipientKind, "recipientKind"),
            RecipientId,
            PropertyId,
            Summary,
            TripReference,
            TargetType,
            TargetId,
            TraceId,
            Utc(OccurredAt),
            ValidUntil is null ? null : Utc(ValidUntil.Value)),
        NotificationId,
        Parse<NotificationEventState>(State, "state"),
        Attempts,
        Utc(NextAttemptAt));

    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static T Parse<T>(string text, string field) where T : struct, Enum =>
        WireEnum.TryParse<T>(text, out var value) ? value : throw new InvalidDataException($"Stored notification event has unknown {field} '{text}'.");
}
