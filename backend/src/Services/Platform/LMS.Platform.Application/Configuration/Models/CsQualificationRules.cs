namespace LMS.Platform.Application.Configuration.Models;

public sealed record CsQualificationRules(
    string EligibleGuestRule,
    string JustificationRule,
    string ApprovalThresholdRule,
    string VerificationSource);
