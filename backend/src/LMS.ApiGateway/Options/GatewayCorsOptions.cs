namespace LMS.ApiGateway.Options;

public sealed class GatewayCorsOptions
{
    public const string SectionName = "Cors";

    // 列表为空时策略保持关闭：不允许任何跨域请求。
    public string[] AllowedOrigins { get; init; } = [];
}
