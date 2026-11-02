using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class NotificationTemplateDocument
{
    [BsonElement("templateId")]
    public string TemplateId { get; init; } = string.Empty;

    [BsonElement("eventType")]
    public string EventType { get; init; } = string.Empty;

    [BsonElement("eventTrigger")]
    public string EventTrigger { get; init; } = string.Empty;

    [BsonElement("channels")]
    public List<string> Channels { get; init; } = [];

    [BsonElement("contentTemplate")]
    public string ContentTemplate { get; init; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; init; }

    public NotificationTemplate ToItem() => new(
        TemplateId,
        StoredValues.ParseEnum<NotificationEventType>(EventType, "eventType"),
        EventTrigger,
        NotificationChannelParser.ParseDistinct(Channels),
        ContentTemplate,
        IsActive);
}
