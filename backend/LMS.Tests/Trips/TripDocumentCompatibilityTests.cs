using LMS.Booking.Infrastructure.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace LMS.Tests.Trips;

public sealed class TripDocumentCompatibilityTests
{
    [Fact]
    public void DeserializesLegacyTripWithAdditionalFields()
    {
        var createdAt = new DateTime(2026, 10, 13, 1, 12, 15, DateTimeKind.Utc);
        var closedAt = new DateTime(2026, 10, 13, 23, 41, 39, DateTimeKind.Utc);
        var document = new BsonDocument
        {
            ["_id"] = "trip_legacy",
            ["tripReference"] = "TR-LEGACY",
            ["propertyId"] = "COD",
            ["guest"] = new BsonDocument
            {
                ["guestId"] = "guest_legacy",
                ["name"] = "Legacy Guest",
                ["tierCode"] = "GOLD",
                ["roomNumber"] = BsonNull.Value,
                ["memberId"] = BsonNull.Value,
                ["phoneMasked"] = "+8536***0088"
            },
            ["route"] = new BsonDocument
            {
                ["pickupLocationId"] = "location_pickup",
                ["pickupName"] = "Pickup",
                ["dropoffLocationId"] = "location_dropoff",
                ["dropoffName"] = "Drop-off",
                ["isCrossBorder"] = false
            },
            ["pickupAt"] = new BsonDateTime(createdAt.AddDays(1)),
            ["pickupMode"] = "SCHEDULED",
            ["serviceType"] = "ONE_WAY",
            ["vehicleClass"] = "LUXURY_SEDAN",
            ["passengers"] = 2,
            ["luggage"] = 0,
            ["specialRequests"] = BsonNull.Value,
            ["status"] = "COMPLETED",
            ["points"] = new BsonDocument
            {
                ["required"] = 3000,
                ["reference"] = BsonNull.Value,
                ["state"] = "REDEEMED"
            },
            ["cancellation"] = BsonNull.Value,
            ["bookedBy"] = new BsonDocument
            {
                ["userId"] = "guest_legacy",
                ["roleCode"] = "CUSTOMER",
                ["name"] = "Legacy Guest"
            },
            ["confirmationLanguage"] = "EN",
            ["createdAt"] = new BsonDateTime(createdAt),
            ["channel"] = "SELF_SERVICE",
            ["product"] = "LIMOUSINE",
            ["assignment"] = new BsonDocument
            {
                ["offerId"] = "offer_legacy",
                ["driverId"] = "driver_legacy",
                ["driverName"] = "Legacy Driver",
                ["vehicleLabel"] = "Legacy Vehicle",
                ["assignedAt"] = new BsonDateTime(createdAt.AddHours(1))
            },
            ["fulfillment"] = new BsonDocument
            {
                ["acceptedAt"] = new BsonDateTime(createdAt.AddHours(1)),
                ["completedAt"] = new BsonDateTime(closedAt)
            },
            ["events"] = new BsonArray(),
            ["activeDriverId"] = BsonNull.Value,
            ["closedAt"] = new BsonDateTime(closedAt),
            ["modifications"] = BsonNull.Value,
            ["version"] = 8L
        };

        var stored = BsonSerializer.Deserialize<TripDocument>(document);
        var trip = stored.ToDomain();

        Assert.Equal("trip_legacy", trip.TripId);
        Assert.Equal("guest_legacy", trip.BookedBy.UserId);
        Assert.Equal("driver_legacy", trip.Assignment?.DriverId);
        Assert.Equal(8L, trip.Version);
    }
}
