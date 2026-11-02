using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Booking.Domain.Overrides;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class QuotaOverrideDocument
{
    [BsonId]
    public string OverrideId { get; init; } = string.Empty;

    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("guestId")]
    public string GuestId { get; init; } = string.Empty;

    [BsonElement("additionalDailyEntitlements")]
    public int AdditionalDailyEntitlements { get; init; }

    [BsonElement("justification")]
    public string Justification { get; init; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("requestedBy")]
    public string RequestedBy { get; init; } = string.Empty;

    [BsonElement("requestedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime RequestedAt { get; init; }

    [BsonElement("decision")]
    public OverrideDecisionDocument? Decision { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static QuotaOverrideDocument FromDomain(QuotaOverride quotaOverride) => new()
    {
        OverrideId = quotaOverride.OverrideId,
        PropertyId = quotaOverride.PropertyId,
        GuestId = quotaOverride.GuestId,
        AdditionalDailyEntitlements = quotaOverride.AdditionalDailyEntitlements,
        Justification = quotaOverride.Justification,
        Status = WireEnum.ToWire(quotaOverride.Status),
        RequestedBy = quotaOverride.RequestedBy,
        RequestedAt = UtcDateTimes.ToStored(quotaOverride.RequestedAt),
        Decision = quotaOverride.Decision is null ? null : OverrideDecisionDocument.FromDomain(quotaOverride.Decision),
        Version = quotaOverride.Version
    };

    public QuotaOverride ToDomain() => new(
        OverrideId,
        PropertyId,
        GuestId,
        AdditionalDailyEntitlements,
        Justification,
        StoredValues.ParseEnum<OverrideStatus>(Status, "status"),
        RequestedBy,
        UtcDateTimes.FromStored(RequestedAt),
        Decision?.ToDomain(),
        Version);
}
