using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Domain.Trips;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class TripEventDocument
{
    [BsonElement("eventId")]
    public string EventId { get; init; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; init; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("actor")]
    public string Actor { get; init; } = string.Empty;

    [BsonElement("occurredAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAt { get; init; }

    [BsonElement("idempotencyKey")]
    public string? IdempotencyKey { get; init; }

    public static TripEventDocument FromDomain(TripEvent tripEvent) => new()
    {
        EventId = tripEvent.EventId,
        Type = WireEnum.ToWire(tripEvent.Type),
        Status = WireEnum.ToWire(tripEvent.Status),
        Actor = tripEvent.Actor,
        OccurredAt = UtcDateTimes.ToStored(tripEvent.OccurredAt),
        IdempotencyKey = tripEvent.IdempotencyKey
    };

    public TripEvent ToDomain() => new(
        EventId,
        StoredValues.ParseEnum<TripEventType>(Type, "events.type"),
        StoredValues.ParseEnum<TripStatus>(Status, "events.status"),
        Actor,
        UtcDateTimes.FromStored(OccurredAt),
        IdempotencyKey);
}
