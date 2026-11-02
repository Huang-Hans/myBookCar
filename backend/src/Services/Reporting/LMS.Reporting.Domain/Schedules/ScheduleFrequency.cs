namespace LMS.Reporting.Domain.Schedules;

// 邮件报告计划的频率，精确对应原型的三个选项。具体的执行时刻、统计窗口与是否继承当前筛选尚未确定（Q08）。
public static class ScheduleFrequency
{
    public const string Daily0800 = "DAILY_0800";
    public const string WeeklyMonday = "WEEKLY_MONDAY";
    public const string MonthlyFirstDay = "MONTHLY_FIRST_DAY";

    public static IReadOnlyList<string> All { get; } = [Daily0800, WeeklyMonday, MonthlyFirstDay];

    public static bool IsValid(string value) => All.Contains(value, StringComparer.Ordinal);
}
