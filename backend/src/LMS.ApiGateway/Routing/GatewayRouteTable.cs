using Yarp.ReverseProxy.Configuration;

namespace LMS.ApiGateway.Routing;

// 网关路由表：按 服务名 + 身份范围 + 资源 的允许列表把 /api/{serviceName}/{identityScope}/v1/{resource} 转发给承载它的服务。
// 身份范围（staff / driver / customer）表达调用对象；认证方案与身份类型由下游服务各自的策略强制，网关只负责转发，不赋予任何权限。
// 未知身份段或未登记资源不转发，没有匹配所有内容的 catch-all。服务地址使用 AppHost 注册的服务名，由服务发现解析，网关不配置固定地址。
// 服务间端点（/internal）不在路由表中，因此不会经网关对外发布。
// 员工登录/刷新（/api/v1/login、/api/v1/refresh）是网关自己的控制器，不经这张表。
internal static class GatewayRouteTable
{
    private const string ApiPrefix = "/api";
    private const string StaffScope = "staff";
    private const string DriverScope = "driver";
    private const string CustomerScope = "customer";
    private const string PlatformClusterId = "platform-api";
    private const string DispatchClusterId = "dispatch-api";
    private const string BookingClusterId = "booking-api";
    private const string NotificationClusterId = "notification-api";
    private const string ReportingClusterId = "reporting-api";
    private const string PlatformServiceName = "platform";
    private const string DispatchServiceName = "dispatch";
    private const string BookingServiceName = "booking";
    private const string NotificationServiceName = "notification";
    private const string ReportingServiceName = "reporting";

    // 司机/客户本地认证入口：未登录即可访问，由 Platform 的认证控制器处理。
    private static readonly string[] AppAuthenticationResources = ["logins", "token-refreshes", "logouts"];

    private static readonly string[] PlatformStaffResources =
    [
        "users",
        "locations",
        "location-orderings",
        "customer-blacklists",
        "audit-records",
        "properties",
        "cancellation-reasons",
        "location-categories",
        "travel-time-indices",
        "notification-templates",
        "notification-channel-configurations",
        "loyalty-qualification-rules",
        "self-booking-rules",
        "self-booking-eligibility-rules",
        "customer-service-qualification-rules",
        "role-permission-references",
        "system-parameters",
        "integration-references",
        "configuration-register",
        "membership-tiers",
        "file-uploads",
        "file-downloads",
        "files",
        "audit-exports",
        "current-user"
    ];

    private static readonly string[] BookingStaffResources =
    [
        "property-quotas",
        "quota-overrides",
        "qualification-references",
        "qualification-audits",
        "cross-border-approvals",
        "cross-border-approval-summaries",
        "cross-border-approval-decisions",
        "blacklist-hits",
        "trips",
        "booking-previews",
        "booking-options",
        "trip-status-summaries",
        "guests",
        "console-notifications",
        "notifications",
        "console-settings",
        "notification-preferences",
        "location-favorites"
    ];

    // me：司机/客户本人资源（me/...），本人标识来自令牌，不是路径或正文参数。
    private static readonly string[] SelfResources = ["me"];

    // 本人资源之外的实时通知 Hub（SignalR 协商与传输都走 notifications/{**remainder}）；
    // 认证由下游 Hub 的策略强制，网关只转发，不记录 access_token 查询值。
    private static readonly string[] SelfResourcesWithNotifications = ["me", "notifications"];

    private static readonly RouteGroup[] RouteGroups =
    [
        new(PlatformClusterId, PlatformServiceName, StaffScope, PlatformStaffResources),
        new(PlatformClusterId, PlatformServiceName, DriverScope, AppAuthenticationResources),
        new(PlatformClusterId, PlatformServiceName, CustomerScope, AppAuthenticationResources),
        new(DispatchClusterId, DispatchServiceName, StaffScope, ["drivers", "vehicles", "shifts", "dispatch-orders", "coupons"]),
        new(DispatchClusterId, DispatchServiceName, DriverScope, SelfResourcesWithNotifications),
        new(BookingClusterId, BookingServiceName, StaffScope, BookingStaffResources),
        new(BookingClusterId, BookingServiceName, DriverScope, SelfResources),
        new(BookingClusterId, BookingServiceName, CustomerScope, SelfResourcesWithNotifications),
        new(NotificationClusterId, NotificationServiceName, StaffScope, ["notification-history"]),
        new(ReportingClusterId, ReportingServiceName, StaffScope, ["reports", "report-schedules", "report-exports", "file-downloads"])
    ];

    public static IReadOnlyList<RouteConfig> Routes { get; } =
        RouteGroups.SelectMany(group => group.Resources.Select(resource => CreateRoute(group, resource))).ToList();

    public static IReadOnlyList<ClusterConfig> Clusters { get; } =
    [
        CreateCluster(PlatformClusterId),
        CreateCluster(DispatchClusterId),
        CreateCluster(BookingClusterId),
        CreateCluster(NotificationClusterId),
        CreateCluster(ReportingClusterId)
    ];

    private static RouteConfig CreateRoute(RouteGroup group, string resource) =>
        new()
        {
            RouteId = $"{group.ClusterId}:{group.IdentityScope}:{resource}",
            ClusterId = group.ClusterId,
            Match = new RouteMatch { Path = $"{ApiPrefix}/{group.ServiceName}/{group.IdentityScope}/v1/{resource}/{{**remainder}}" }
        };

    private static ClusterConfig CreateCluster(string clusterId) =>
        new()
        {
            ClusterId = clusterId,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["primary"] = new() { Address = $"http://{clusterId}" }
            }
        };

    private sealed record RouteGroup(string ClusterId, string ServiceName, string IdentityScope, string[] Resources);
}
