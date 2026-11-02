namespace LMS.Platform.Infrastructure.Quotas;

// Booking 服务地址（读取每日限额）。默认使用服务名，由服务发现解析。
public sealed class BookingClientOptions
{
    public const string SectionName = "Booking";

    public string BaseAddress { get; init; } = "http://booking-api";
}
