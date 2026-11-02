using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Domain.Approvals;
using LMS.Booking.Domain.Common;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class ApprovalDecisionDocument
{
    [BsonElement("decision")]
    public string Decision { get; init; } = string.Empty;

    [BsonElement("comments")]
    public string Comments { get; init; } = string.Empty;

    [BsonElement("decidedBy")]
    public string DecidedBy { get; init; } = string.Empty;

    [BsonElement("decidedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime DecidedAt { get; init; }

    public static ApprovalDecisionDocument FromDomain(ApprovalDecision decision) => new()
    {
        Decision = WireEnum.ToWire(decision.Decision),
        Comments = decision.Comments,
        DecidedBy = decision.DecidedBy,
        DecidedAt = UtcDateTimes.ToStored(decision.DecidedAt)
    };

    public ApprovalDecision ToDomain() =>
        new(StoredValues.ParseEnum<ReviewDecision>(Decision, "decision.decision"), Comments, DecidedBy, UtcDateTimes.FromStored(DecidedAt));
}
