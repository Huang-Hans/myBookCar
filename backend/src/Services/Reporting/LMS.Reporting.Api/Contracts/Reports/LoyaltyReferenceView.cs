namespace LMS.Reporting.Api.Contracts.Reports;

/// <summary>
/// 外部权益系统的只读引用。
/// </summary>
/// <param name="RedemptionId">兑换标识；行程上没有记录时为 null。</param>
/// <param name="PointsRequired">所需积分。</param>
/// <param name="Entitlement">权益说明；目前没有来源，为 null。</param>
/// <param name="RedemptionState">兑换状态文本。</param>
public sealed record LoyaltyReferenceView(string? RedemptionId, long PointsRequired, string? Entitlement, string RedemptionState);
