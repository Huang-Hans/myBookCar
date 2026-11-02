namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 外部忠诚度计划的兑换与权益参考快照；本地不据此扣减积分。
/// </summary>
/// <param name="RedemptionId">外部兑换引用。</param>
/// <param name="PointsRequired">外部所需积分参考值，非负整数，不是本地扣减余额。</param>
/// <param name="Entitlement">外部权益描述。</param>
/// <param name="RedemptionState">外部状态标签（如 Points Reserved）；不是本地状态机枚举。</param>
public sealed record LoyaltyReferenceView(string RedemptionId, long PointsRequired, string Entitlement, string RedemptionState);
