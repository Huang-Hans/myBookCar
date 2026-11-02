using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace LMS.ApiGateway.Routing;

// 网关的代理配置在进程生命周期内固定不变（见 GatewayRouteTable），因此变更令牌永远不会触发。
internal sealed class GatewayProxyConfigProvider : IProxyConfigProvider
{
    private readonly GatewayProxyConfig config = new(GatewayRouteTable.Routes, GatewayRouteTable.Clusters);

    public IProxyConfig GetConfig() => config;

    private sealed class GatewayProxyConfig(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters) : IProxyConfig
    {
        public IReadOnlyList<RouteConfig> Routes { get; } = routes;

        public IReadOnlyList<ClusterConfig> Clusters { get; } = clusters;

        public IChangeToken ChangeToken { get; } = new CancellationChangeToken(CancellationToken.None);
    }
}
