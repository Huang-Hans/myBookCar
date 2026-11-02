namespace LMS.Reporting.Domain.Rows;

// 外部权益系统的只读引用，不是本服务创造的事实。兑换标识与权益说明在行程上尚无来源时为 null，不用演示值填充。
public sealed record LoyaltyReference(string? RedemptionId, long PointsRequired, string? Entitlement, string RedemptionState);
