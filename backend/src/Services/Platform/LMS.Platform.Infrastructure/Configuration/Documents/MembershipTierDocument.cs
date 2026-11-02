using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class MembershipTierDocument
{
    [BsonElement("tierCode")]
    public string TierCode { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("monthlyEntitlementQuota")]
    public int MonthlyEntitlementQuota { get; init; }

    [BsonElement("bookingPriority")]
    public string BookingPriority { get; init; } = string.Empty;

    [BsonElement("vehicleUpgradeReference")]
    public string VehicleUpgradeReference { get; init; } = string.Empty;

    [BsonElement("advanceBookingWindowHours")]
    public int AdvanceBookingWindowHours { get; init; }

    [BsonElement("cancellationFlexibilityReference")]
    public string CancellationFlexibilityReference { get; init; } = string.Empty;

    public MembershipTier ToItem() => new(
        TierCode,
        Name,
        MonthlyEntitlementQuota,
        StoredValues.ParseEnum<BookingPriority>(BookingPriority, "bookingPriority"),
        VehicleUpgradeReference,
        AdvanceBookingWindowHours,
        CancellationFlexibilityReference);
}
