namespace LMS.Booking.Infrastructure.Remote;

// Dispatch 服务地址，默认使用 Aspire 注册的服务名，由服务发现解析。
public sealed class DispatchClientOptions
{
    public const string SectionName = "Dispatch";

    public string BaseAddress { get; init; } = "http://dispatch-api";
}
