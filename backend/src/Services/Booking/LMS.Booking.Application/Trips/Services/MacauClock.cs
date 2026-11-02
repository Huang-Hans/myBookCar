namespace LMS.Booking.Application.Trips.Services;

// 业务时区（Asia/Macau）：单号日期与配额日窗口都按此时区切分。
internal static class MacauClock
{
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Macau");

    public static DateOnly DateOf(DateTimeOffset utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, TimeZone).DateTime);

    public static (DateTimeOffset DayStart, DateTimeOffset DayEnd) DayWindow(DateOnly macauDate)
    {
        var dayStart = TimeZoneInfo.ConvertTimeToUtc(macauDate.ToDateTime(TimeOnly.MinValue), TimeZone);

        return (dayStart, dayStart.AddDays(1));
    }
}
