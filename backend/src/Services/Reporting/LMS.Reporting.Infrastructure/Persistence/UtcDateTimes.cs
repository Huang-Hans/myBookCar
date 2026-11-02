namespace LMS.Reporting.Infrastructure.Persistence;

// 持久时间一律使用 UTC。
internal static class UtcDateTimes
{
    public static DateTime ToStored(DateTimeOffset value) => value.UtcDateTime;

    public static DateTimeOffset FromStored(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
