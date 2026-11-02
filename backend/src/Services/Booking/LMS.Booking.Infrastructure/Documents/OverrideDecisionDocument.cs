using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Domain.Common;
using LMS.Booking.Domain.Overrides;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class OverrideDecisionDocument
{
    [BsonElement("decision")]
    public string Decision { get; init; } = string.Empty;

    [BsonElement("decidedBy")]
    public string DecidedBy { get; init; } = string.Empty;

    [BsonElement("decidedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime DecidedAt { get; init; }

    public static OverrideDecisionDocument FromDomain(OverrideDecision decision) => new()
    {
        Decision = WireEnum.ToWire(decision.Decision),
        DecidedBy = decision.DecidedBy,
        DecidedAt = UtcDateTimes.ToStored(decision.DecidedAt)
    };

    public OverrideDecision ToDomain() =>
        new(StoredValues.ParseEnum<ReviewDecision>(Decision, "decision.decision"), DecidedBy, UtcDateTimes.FromStored(DecidedAt));
}
