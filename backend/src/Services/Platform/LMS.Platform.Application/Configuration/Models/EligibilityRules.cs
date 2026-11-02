namespace LMS.Platform.Application.Configuration.Models;

public sealed record EligibilityRules(
    IReadOnlyList<string> EligibleTierCodes,
    string EntitlementRule,
    string PointsValidationRule,
    string RiskCheckRule,
    string ExhaustionRule);
