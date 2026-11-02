namespace LMS.Platform.Application.Configuration.Models;

public sealed record LoyaltyQualificationRules(
    string Authority,
    string PointsEligibility,
    string PlayQualificationReference,
    string VisitRequirementReference,
    string SpecialDesignationReference,
    string PointsExpiryReference);
