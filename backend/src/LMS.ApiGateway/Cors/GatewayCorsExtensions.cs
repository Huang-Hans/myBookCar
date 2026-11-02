using LMS.ApiGateway.Configuration;
using LMS.ApiGateway.Options;
using LMS.BuildingBlocks.Http.Contracts;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace LMS.ApiGateway.Cors;

internal static class GatewayCorsExtensions
{
    public const string PolicyName = "AllowFrontend";

    // 校验在主机启动时执行，配置缺失或格式错误会在网关开始服务前终止启动。
    public static IServiceCollection AddGatewayCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<GatewayCorsOptions>, GatewayCorsOptionsValidator>();

        services.AddOptions<GatewayCorsOptions>()
            .Bind(configuration.GetSection(GatewayCorsOptions.SectionName))
            .ValidateOnStart();

        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<GatewayCorsOptions>>(AddFrontendPolicy);

        return services;
    }

    // 只允许列表内的来源；列表项可写成 https://*.example.com 匹配任意子域。暴露 X-Trace-Id，便于浏览器端记录并反馈该值。
    private static void AddFrontendPolicy(CorsOptions cors, IOptions<GatewayCorsOptions> gatewayCors) =>
        cors.AddPolicy(PolicyName, policy => policy
            .WithOrigins(gatewayCors.Value.AllowedOrigins)
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders(ApiHeaderNames.TraceId));
}
