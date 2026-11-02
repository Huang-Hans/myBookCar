using LMS.Platform.Application.Configuration.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class CsQualificationRulesDocument
{
    [BsonElement("eligibleGuestRule")]
    public string EligibleGuestRule { get; init; } = string.Empty;

    [BsonElement("justificationRule")]
    public string JustificationRule { get; init; } = string.Empty;

    [BsonElement("approvalThresholdRule")]
    public string ApprovalThresholdRule { get; init; } = string.Empty;

    [BsonElement("verificationSource")]
    public string VerificationSource { get; init; } = string.Empty;

    public CsQualificationRules ToModel() => new(EligibleGuestRule, JustificationRule, ApprovalThresholdRule, VerificationSource);
}
