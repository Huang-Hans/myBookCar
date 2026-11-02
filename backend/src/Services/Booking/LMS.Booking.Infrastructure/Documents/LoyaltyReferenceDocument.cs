using LMS.Booking.Domain.Approvals;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class LoyaltyReferenceDocument
{
    [BsonElement("redemptionId")]
    public string RedemptionId { get; init; } = string.Empty;

    [BsonElement("pointsRequired")]
    public long PointsRequired { get; init; }

    [BsonElement("entitlement")]
    public string Entitlement { get; init; } = string.Empty;

    [BsonElement("redemptionState")]
    public string RedemptionState { get; init; } = string.Empty;

    public static LoyaltyReferenceDocument FromDomain(LoyaltyReference loyalty) => new()
    {
        RedemptionId = loyalty.RedemptionId,
        PointsRequired = loyalty.PointsRequired,
        Entitlement = loyalty.Entitlement,
        RedemptionState = loyalty.RedemptionState
    };

    public LoyaltyReference ToDomain() => new(RedemptionId, PointsRequired, Entitlement, RedemptionState);
}
