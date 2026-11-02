using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Dispatch.Domain.Drivers;

// 司机能否作为派单候选。候选查询与创建邀请使用同一规则：物业一致、状态启用、可用状态为 AVAILABLE。
// Missing availability is ineligible. Vehicle binding and grade are checked separately by the offer guard.
public static class DispatchCandidateRules
{
    // 返回 null 表示可以作为候选；否则给出原因。
    public static FieldIssue? Check(Driver driver, string tripPropertyId)
    {
        if (driver.Status != DriverStatus.Active || driver.PropertyId != tripPropertyId)
        {
            return new FieldIssue("driverId", "Driver must be active and belong to the trip property");
        }

        return driver.Availability switch
        {
            DriverAvailability.Available => null,
            DriverAvailability.OnTrip => new FieldIssue("driverId", "Driver is currently on a trip. Wait until the trip ends or select another driver."),
            DriverAvailability.Break => new FieldIssue("driverId", "Driver is on a break. Ask the driver to switch to Available before sending an invitation."),
            DriverAvailability.OffDuty => new FieldIssue("driverId", "Driver is off duty. Ask the driver to switch to Available before sending an invitation."),
            _ => new FieldIssue("driverId", "Driver availability has not been set. Ask the driver to switch to Available before sending an invitation.")
        };
    }
}
