namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 客务升级资格规则视图，描述客户服务处理升级/例外时依据的规则。
/// </summary>
/// <param name="EligibleGuestRule">符合升级资格的客人规则说明。</param>
/// <param name="JustificationRule">升级理由（Justification）的填写要求说明。</param>
/// <param name="ApprovalThresholdRule">审批阈值说明，不含本地数字阈值。</param>
/// <param name="VerificationSource">资格核验来源说明。</param>
public sealed record CsQualificationRulesView(
    string EligibleGuestRule,
    string JustificationRule,
    string ApprovalThresholdRule,
    string VerificationSource);
