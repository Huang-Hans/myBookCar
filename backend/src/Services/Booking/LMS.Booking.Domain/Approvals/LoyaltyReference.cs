namespace LMS.Booking.Domain.Approvals;

// 外部兑换与权益参考。积分只是外部参考值，不在本地扣减；redemptionState 是外部状态标签，不是本地状态机。
public sealed record LoyaltyReference(string RedemptionId, long PointsRequired, string Entitlement, string RedemptionState);
