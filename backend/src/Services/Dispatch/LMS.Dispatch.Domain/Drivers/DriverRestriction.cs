namespace LMS.Dispatch.Domain.Drivers;

// 司机黑名单展示信息，由停用（INACTIVE）或暂停（SUSPENDED）动作产生；不增加第二个司机黑名单集合。EffectiveDate 是业务日期。
public sealed record DriverRestriction(string? Reason, DateOnly? EffectiveDate)
{
    public const string DeactivationReason = "Deactivated by Operations";
    public const string SuspensionReason = "Suspended by Operations";

    // 进入黑名单的状态；与 For 保持一致，供按状态筛选使用。
    public static IReadOnlyList<DriverStatus> RestrictedStatuses { get; } = [DriverStatus.Inactive, DriverStatus.Suspended];

    // ACTIVE 没有限制，返回 null。
    public static DriverRestriction? For(DriverStatus status, DateOnly businessDate) => status switch
    {
        DriverStatus.Inactive => new DriverRestriction(DeactivationReason, businessDate),
        DriverStatus.Suspended => new DriverRestriction(SuspensionReason, businessDate),
        _ => null
    };
}
