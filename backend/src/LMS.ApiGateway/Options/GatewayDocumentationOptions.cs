using System.ComponentModel.DataAnnotations;

namespace LMS.ApiGateway.Options;

// 下游接口文档的取用与缓存策略。文档只用于排查与调试，因此以“可用”优先于“实时”。
public sealed class GatewayDocumentationOptions
{
    public const string SectionName = "ApiDocumentation";

    // 下游文档的缓存时间，期间不再请求下游。
    [Range(1, 3600)]
    public int CacheSeconds { get; init; } = 60;

    // 下游取文档失败后的退避时间，期间不再重试，避免下游故障时每次打开文档页都等待超时。
    [Range(1, 3600)]
    public int FailureBackoffSeconds { get; init; } = 10;

    // 单次取下游文档的超时。
    [Range(1, 60)]
    public int FetchTimeoutSeconds { get; init; } = 5;

    // 下游持续不可用时，最多继续使用上一次成功取得的文档多久。
    [Range(1, 86400)]
    public int MaxStaleSeconds { get; init; } = 3600;
}
