namespace LMS.Reporting.Domain.Building;

// 司机一天的空闲与加班（口径待业务确认 Q1）：
// 忙碌时间 = 各行程从出发到完成的区间合并；空闲 = 当值班次总时长 − 班次内的忙碌时间；加班 = 落在所有班次之外的忙碌时间。
public sealed record DriverWorkTime(TimeSpan Idle, TimeSpan Overtime)
{
    public static DriverWorkTime Of(IEnumerable<TimeInterval> shifts, IEnumerable<TimeInterval> busy)
    {
        var shiftWindows = TimeInterval.Merge(shifts);
        var busyWindows = TimeInterval.Merge(busy);
        var shiftTotal = Sum(shiftWindows.Select(window => window.Duration));
        var busyTotal = Sum(busyWindows.Select(window => window.Duration));
        var busyInShift = Sum(busyWindows.SelectMany(window => shiftWindows.Select(window.OverlapWith)));

        return new DriverWorkTime(shiftTotal - busyInShift, busyTotal - busyInShift);
    }

    private static TimeSpan Sum(IEnumerable<TimeSpan> values) => values.Aggregate(TimeSpan.Zero, (total, value) => total + value);
}
