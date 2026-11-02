using LMS.Platform.Application.Configuration.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class SelfBookingRulesDocument
{
    [BsonElement("minimumLeadTimeHours")]
    public int MinimumLeadTimeHours { get; init; }

    [BsonElement("maximumAdvanceDays")]
    public int MaximumAdvanceDays { get; init; }

    [BsonElement("maximumActiveRequestsPerGuestPerDay")]
    public int MaximumActiveRequestsPerGuestPerDay { get; init; }

    [BsonElement("propertyAccessRule")]
    public string PropertyAccessRule { get; init; } = string.Empty;

    [BsonElement("eligibilitySource")]
    public string EligibilitySource { get; init; } = string.Empty;

    public SelfBookingRules ToModel() => new(
        MinimumLeadTimeHours, MaximumAdvanceDays, MaximumActiveRequestsPerGuestPerDay, PropertyAccessRule, EligibilitySource);
}
