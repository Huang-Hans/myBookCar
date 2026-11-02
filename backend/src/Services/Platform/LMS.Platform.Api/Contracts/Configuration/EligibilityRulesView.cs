namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 自助预约资格规则视图，描述准入等级与各项核验规则。
/// </summary>
/// <param name="EligibleTierCodes">允许自助预约的会员等级代码列表，已去重。</param>
/// <param name="EntitlementRule">权益要求说明。</param>
/// <param name="PointsValidationRule">积分来源核验说明。</param>
/// <param name="RiskCheckRule">客户限制检查说明。</param>
/// <param name="ExhaustionRule">物业配额耗尽时的处理说明。</param>
public sealed record EligibilityRulesView(
    IReadOnlyList<string> EligibleTierCodes,
    string EntitlementRule,
    string PointsValidationRule,
    string RiskCheckRule,
    string ExhaustionRule);
