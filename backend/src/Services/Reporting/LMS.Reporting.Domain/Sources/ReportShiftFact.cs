namespace LMS.Reporting.Domain.Sources;

// 来自 Dispatch 的一条班次。只有当值（ON_DUTY）的班次计入空闲与加班；夜班挂在开始那天的业务日期上。DriverName 是司机当前姓名，主档缺失时为 null。
public sealed record ReportShiftFact(string DriverId, string? DriverName, string PropertyId, DateOnly BusinessDate, ReportShiftPeriod Period, bool IsOnDuty);
