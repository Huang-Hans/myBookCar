using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Dispatch.Domain.Drivers;

// 司机本人可执行的工作状态规则。ON_TRIP 只由有效指派驱动，不能自报；执行中的行程期间也不能切换为休息或下班。
// 停用 / 暂停的司机不能自行恢复接单资格。
public static class DriverSelfRules
{
    private static readonly DriverAvailability[] SelfSelectable =
        [DriverAvailability.Available, DriverAvailability.Break, DriverAvailability.OffDuty];

    public static bool IsSelfSelectable(DriverAvailability availability) => SelfSelectable.Contains(availability);

    public static bool CanAcceptOffers(Driver driver) =>
        driver.Status == DriverStatus.Active && driver.Availability == DriverAvailability.Available;

    // 返回 null 表示允许；否则给出不允许的原因。
    public static FieldIssue? CheckAvailabilityChange(Driver driver, DriverAvailability target)
    {
        if (driver.Status != DriverStatus.Active)
        {
            return new FieldIssue("availability", "Only an active driver can change availability");
        }

        if (!IsSelfSelectable(target))
        {
            return new FieldIssue("availability", "Must be AVAILABLE, BREAK or OFF_DUTY");
        }

        return driver.Availability == DriverAvailability.OnTrip
            ? new FieldIssue("availability", "Availability cannot be changed during an active trip")
            : null;
    }
}
