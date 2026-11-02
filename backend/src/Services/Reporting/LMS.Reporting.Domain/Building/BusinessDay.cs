using LMS.Reporting.Domain.Sources;

namespace LMS.Reporting.Domain.Building;

// 澳门业务日（UTC+8，无夏令时）。行程按计划接车时间归入业务日；班次窗口与 Dispatch 的固定班次一致。
public static class BusinessDay
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(8);

    public static DateTimeOffset StartOf(DateOnly day) => new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), Offset).ToUniversalTime();

    public static DateTimeOffset EndOf(DateOnly day) => StartOf(day.AddDays(1));

    public static DateOnly Of(DateTimeOffset instant) => DateOnly.FromDateTime(instant.ToOffset(Offset).DateTime);

    public static TimeInterval ShiftWindow(DateOnly day, ReportShiftPeriod period)
    {
        var start = StartOf(day);

        return period switch
        {
            ReportShiftPeriod.Morning => new TimeInterval(start.AddHours(7), start.AddHours(15)),
            ReportShiftPeriod.Afternoon => new TimeInterval(start.AddHours(15), start.AddHours(23)),
            ReportShiftPeriod.Night => new TimeInterval(start.AddHours(23), start.AddHours(31)),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown shift period.")
        };
    }
}
