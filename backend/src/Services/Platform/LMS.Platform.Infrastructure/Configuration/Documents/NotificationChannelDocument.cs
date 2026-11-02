using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class NotificationChannelDocument
{
    [BsonElement("eventType")]
    public string EventType { get; init; } = string.Empty;

    [BsonElement("channels")]
    public List<string> Channels { get; init; } = [];

    [BsonElement("eventTrigger")]
    public string EventTrigger { get; init; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; init; }

    public NotificationChannelConfiguration ToItem() => new(
        StoredValues.ParseEnum<NotificationEventType>(EventType, "eventType"),
        NotificationChannelParser.ParseDistinct(Channels),
        EventTrigger,
        IsActive);
}
