namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 会员积分资格规则视图，描述外部会员系统的权威与各项资格来源。
/// </summary>
/// <param name="Authority">外部会员系统的权威说明。</param>
/// <param name="PointsEligibility">积分与权益核验说明。</param>
/// <param name="PlayQualificationReference">博彩资格结果引用说明，不含投注金额。</param>
/// <param name="VisitRequirementReference">到访资格来源说明。</param>
/// <param name="SpecialDesignationReference">VIP 指定来源说明。</param>
/// <param name="PointsExpiryReference">积分失效来源说明。</param>
public sealed record LoyaltyQualificationRulesView(
    string Authority,
    string PointsEligibility,
    string PlayQualificationReference,
    string VisitRequirementReference,
    string SpecialDesignationReference,
    string PointsExpiryReference);
