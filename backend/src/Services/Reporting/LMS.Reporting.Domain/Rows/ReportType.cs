namespace LMS.Reporting.Domain.Rows;

// 五类运营报表（对外字面值 DAILY_JOB_SUMMARY / TRIP_DETAIL / DRIVER_UTILIZATION / VEHICLE_UTILIZATION / CANCELLATION）。
public enum ReportType
{
    DailyJobSummary,
    TripDetail,
    DriverUtilization,
    VehicleUtilization,
    Cancellation
}
