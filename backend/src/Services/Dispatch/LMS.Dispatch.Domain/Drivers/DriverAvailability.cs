namespace LMS.Dispatch.Domain.Drivers;

// 司机可用状态，只读，由可信同步来源写入；没有同步值时不默认任何状态。
public enum DriverAvailability
{
    Available,
    OnTrip,
    OffDuty,
    Break
}
