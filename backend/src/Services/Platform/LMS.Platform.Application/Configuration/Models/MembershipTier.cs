namespace LMS.Platform.Application.Configuration.Models;

// 上传配置中的会员等级与权益矩阵。有效会员数来自外部会员系统（Q03），不在上传配置中，因此这里不保存。
public sealed record MembershipTier(
    string TierCode,
    string Name,
    int MonthlyEntitlementQuota,
    BookingPriority BookingPriority,
    string VehicleUpgradeReference,
    int AdvanceBookingWindowHours,
    string CancellationFlexibilityReference);
