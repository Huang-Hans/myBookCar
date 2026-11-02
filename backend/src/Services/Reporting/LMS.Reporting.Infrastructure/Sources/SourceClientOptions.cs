namespace LMS.Reporting.Infrastructure.Sources;

// 报表来源服务的地址，默认使用 Aspire 注册的服务名，由服务发现解析。
public sealed class SourceClientOptions
{
    public const string SectionName = "ReportSources";

    public string BookingBaseAddress { get; init; } = "http://booking-api";

    public string DispatchBaseAddress { get; init; } = "http://dispatch-api";

    public string PlatformBaseAddress { get; init; } = "http://platform-api";

    public int BookingBatchSize { get; init; } = 200;
}
