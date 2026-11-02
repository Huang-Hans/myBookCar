namespace LMS.Platform.Infrastructure.Authentication;

// Dispatch 服务地址（司机凭据核对）。默认使用服务名，由服务发现解析。
public sealed class DispatchClientOptions
{
    public const string SectionName = "Dispatch";

    public string BaseAddress { get; init; } = "http://dispatch-api";
}
