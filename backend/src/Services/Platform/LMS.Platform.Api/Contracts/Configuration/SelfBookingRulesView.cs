namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 自助预约规则视图，给出提前量、数量上限与资格来源。
/// </summary>
/// <param name="MinimumLeadTimeHours">最短提前预约小时数（>=0）。</param>
/// <param name="MaximumAdvanceDays">最多可提前预约的天数（>=0），不要与等级权益窗口混淆。</param>
/// <param name="MaximumActiveRequestsPerGuestPerDay">每位客人每日可同时存在的有效预约申请上限（>=0）。</param>
/// <param name="PropertyAccessRule">可预约物业范围说明。</param>
/// <param name="EligibilitySource">外部资格来源。</param>
public sealed record SelfBookingRulesView(
    int MinimumLeadTimeHours,
    int MaximumAdvanceDays,
    int MaximumActiveRequestsPerGuestPerDay,
    string PropertyAccessRule,
    string EligibilitySource);
