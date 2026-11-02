using LMS.BuildingBlocks.Core.Wire;
using LMS.Notification.Domain.History;
using LMS.Notification.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Notification.Infrastructure.Documents;

internal sealed class NotificationHistoryDocument
{
    [BsonId]
    public string NotificationId { get; init; } = string.Empty;

    [BsonElement("recipient")]
    public string Recipient { get; init; } = string.Empty;

    [BsonElement("channel")]
    public string Channel { get; init; } = string.Empty;

    [BsonElement("occurredAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAt { get; init; }

    [BsonElement("eventType")]
    public string EventType { get; init; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("contentPreview")]
    public string ContentPreview { get; init; } = string.Empty;

    public NotificationHistoryEntry ToDomain() => new(
        NotificationId,
        Recipient,
        StoredValues.ParseEnum<NotificationChannel>(Channel, "channel"),
        UtcDateTimes.FromStored(OccurredAt),
        StoredValues.ParseEnum<NotificationEventType>(EventType, "eventType"),
        StoredValues.ParseEnum<NotificationStatus>(Status, "status"),
        ContentPreview);
}
