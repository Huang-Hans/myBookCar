using LMS.Dispatch.Domain.Offers;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class OfferTripSummaryDocument
{
    [BsonElement("pickupAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime PickupAt { get; init; }

    [BsonElement("pickupName")]
    public string PickupName { get; init; } = string.Empty;

    [BsonElement("dropoffName")]
    public string DropoffName { get; init; } = string.Empty;

    [BsonElement("passengers")]
    public int Passengers { get; init; }

    [BsonElement("vehicleClass")]
    public string VehicleClass { get; init; } = string.Empty;

    public static OfferTripSummaryDocument FromDomain(OfferTripSummary summary) => new()
    {
        PickupAt = UtcDateTimes.ToStored(summary.PickupAt),
        PickupName = summary.PickupName,
        DropoffName = summary.DropoffName,
        Passengers = summary.Passengers,
        VehicleClass = summary.VehicleClass
    };

    public OfferTripSummary ToDomain() =>
        new(UtcDateTimes.FromStored(PickupAt), PickupName, DropoffName, Passengers, VehicleClass);
}
