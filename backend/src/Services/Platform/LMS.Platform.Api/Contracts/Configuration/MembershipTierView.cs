namespace LMS.Platform.Api.Contracts.Configuration;

// ActiveMemberCount 来自外部会员参考（Q03），未接入时省略，不使用演示数量。
/// <summary>
/// 会员等级视图，给出等级代码、权益配额与预约窗口。
/// </summary>
/// <param name="TierCode">等级代码（上传稳定代码，当前 SILVER/GOLD/PLATINUM/DIAMOND/VIP，外部映射待确认）。</param>
/// <param name="Name">等级显示名称。</param>
/// <param name="ActiveMemberCount">外部会员参考的活跃会员数；未接入时为空并省略，不使用演示数量。</param>
/// <param name="MonthlyEntitlementQuota">等级矩阵中的每月权益配额（>=0），不是物业月配额。</param>
/// <param name="BookingPriority">预约优先级，STANDARD / PRIORITY。</param>
/// <param name="VehicleUpgradeReference">车辆升级权益说明。</param>
/// <param name="AdvanceBookingWindowHours">等级矩阵中的提前预约窗口小时数（>=0）。</param>
/// <param name="CancellationFlexibilityReference">取消灵活性说明。</param>
public sealed record MembershipTierView(
    string TierCode,
    string Name,
    long? ActiveMemberCount,
    int MonthlyEntitlementQuota,
    string BookingPriority,
    string VehicleUpgradeReference,
    int AdvanceBookingWindowHours,
    string CancellationFlexibilityReference);
