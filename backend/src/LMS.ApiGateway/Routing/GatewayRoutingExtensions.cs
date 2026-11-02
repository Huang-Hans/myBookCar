using Yarp.ReverseProxy.Configuration;

namespace LMS.ApiGateway.Routing;

internal static class GatewayRoutingExtensions
{
    public static IServiceCollection AddGatewayReverseProxy(this IServiceCollection services)
    {
        // 目标地址是服务名（如 http://platform-api），由服务发现解析为实际地址。
        services.AddReverseProxy().AddServiceDiscoveryDestinationResolver();
        services.AddSingleton<IProxyConfigProvider, GatewayProxyConfigProvider>();

        return services;
    }
}
