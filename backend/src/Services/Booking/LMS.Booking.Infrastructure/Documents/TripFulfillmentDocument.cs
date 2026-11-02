using LMS.Booking.Domain.Trips;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class TripFulfillmentDocument
{
    [BsonElement("acceptedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? AcceptedAt { get; init; }

    [BsonElement("enRouteAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? EnRouteAt { get; init; }

    [BsonElement("arrivedAtPickupAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ArrivedAtPickupAt { get; init; }

    [BsonElement("pickedUpAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? PickedUpAt { get; init; }

    [BsonElement("inTransitAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? InTransitAt { get; init; }

    [BsonElement("arrivedAtDestinationAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ArrivedAtDestinationAt { get; init; }

    [BsonElement("completedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CompletedAt { get; init; }

    public static TripFulfillmentDocument FromDomain(TripFulfillment fulfillment) => new()
    {
        AcceptedAt = ToStored(fulfillment.AcceptedAt),
        EnRouteAt = ToStored(fulfillment.EnRouteAt),
        ArrivedAtPickupAt = ToStored(fulfillment.ArrivedAtPickupAt),
        PickedUpAt = ToStored(fulfillment.PickedUpAt),
        InTransitAt = ToStored(fulfillment.InTransitAt),
        ArrivedAtDestinationAt = ToStored(fulfillment.ArrivedAtDestinationAt),
        CompletedAt = ToStored(fulfillment.CompletedAt)
    };

    public TripFulfillment ToDomain() => new(
        FromStored(AcceptedAt),
        FromStored(EnRouteAt),
        FromStored(ArrivedAtPickupAt),
        FromStored(PickedUpAt),
        FromStored(InTransitAt),
        FromStored(ArrivedAtDestinationAt),
        FromStored(CompletedAt));

    private static DateTime? ToStored(DateTimeOffset? value) => value is null ? null : UtcDateTimes.ToStored(value.Value);

    private static DateTimeOffset? FromStored(DateTime? value) => value is null ? null : UtcDateTimes.FromStored(value.Value);
}
