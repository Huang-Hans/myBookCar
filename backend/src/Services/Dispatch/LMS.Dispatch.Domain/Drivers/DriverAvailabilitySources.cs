namespace LMS.Dispatch.Domain.Drivers;

// 可用状态的写入来源标签，随 availabilitySource 对外展示，用于区分司机自报与履约事实驱动。
public static class DriverAvailabilitySources
{
    public const string SelfReported = "DRIVER_SELF_REPORT";
    public const string TripAssignment = "TRIP_ASSIGNMENT";
    public const string TripReconciliation = "TRIP_RECONCILIATION";
    public const string TripRelease = "TRIP_RELEASE";
}
