namespace LMS.Reporting.Infrastructure.Platform;

// Platform 服务的地址，默认使用 Aspire 注册的服务名，由服务发现解析。
public sealed class PlatformClientOptions
{
    public const string SectionName = "Platform";

    public string BaseAddress { get; init; } = "http://platform-api";
}
