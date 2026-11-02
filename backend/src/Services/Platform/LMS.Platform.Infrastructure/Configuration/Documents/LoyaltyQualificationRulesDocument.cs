using LMS.Platform.Application.Configuration.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class LoyaltyQualificationRulesDocument
{
    [BsonElement("authority")]
    public string Authority { get; init; } = string.Empty;

    [BsonElement("pointsEligibility")]
    public string PointsEligibility { get; init; } = string.Empty;

    [BsonElement("playQualificationReference")]
    public string PlayQualificationReference { get; init; } = string.Empty;

    [BsonElement("visitRequirementReference")]
    public string VisitRequirementReference { get; init; } = string.Empty;

    [BsonElement("specialDesignationReference")]
    public string SpecialDesignationReference { get; init; } = string.Empty;

    [BsonElement("pointsExpiryReference")]
    public string PointsExpiryReference { get; init; } = string.Empty;

    public LoyaltyQualificationRules ToModel() => new(
        Authority, PointsEligibility, PlayQualificationReference, VisitRequirementReference, SpecialDesignationReference, PointsExpiryReference);
}
