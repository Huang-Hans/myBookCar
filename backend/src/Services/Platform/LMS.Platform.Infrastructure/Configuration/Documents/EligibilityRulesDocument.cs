using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class EligibilityRulesDocument
{
    [BsonElement("eligibleTierCodes")]
    public List<string> EligibleTierCodes { get; init; } = [];

    [BsonElement("entitlementRule")]
    public string EntitlementRule { get; init; } = string.Empty;

    [BsonElement("pointsValidationRule")]
    public string PointsValidationRule { get; init; } = string.Empty;

    [BsonElement("riskCheckRule")]
    public string RiskCheckRule { get; init; } = string.Empty;

    [BsonElement("exhaustionRule")]
    public string ExhaustionRule { get; init; } = string.Empty;

    public EligibilityRules ToModel()
    {
        if (EligibleTierCodes.Distinct(StringComparer.Ordinal).Count() != EligibleTierCodes.Count)
        {
            throw new InvalidStoredDataException("Eligible tier codes must be unique.");
        }

        return new EligibilityRules(EligibleTierCodes, EntitlementRule, PointsValidationRule, RiskCheckRule, ExhaustionRule);
    }
}
