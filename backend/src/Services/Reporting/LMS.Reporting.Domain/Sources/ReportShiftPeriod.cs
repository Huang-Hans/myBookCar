namespace LMS.Reporting.Domain.Sources;

// 班次时段（与 Dispatch 的固定班次一致）：早班 07–15、午班 15–23、夜班 23–次日 07。
public enum ReportShiftPeriod
{
    Morning,
    Afternoon,
    Night
}
