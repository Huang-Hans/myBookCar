namespace LMS.Dispatch.Infrastructure.Booking;

// Booking 服务地址，默认使用 Aspire 注册的服务名，由服务发现解析。
public sealed class BookingClientOptions
{
    public const string SectionName = "Booking";

    public string BaseAddress { get; init; } = "http://booking-api";
}
