namespace LMS.BuildingBlocks.Core.Notifications;

// 三个实时通知 Hub 的精确路径与客户端方法名。路径经 API Gateway 的 notifications 资源转发。
// 浏览器的 WebSocket 无法设置 Authorization 头，只有这些路径（及其协商路径）允许从查询串读取 access_token。
public static class NotificationHubRoutes
{
    public const string Staff = "/api/booking/staff/v1/notifications";
    public const string Customer = "/api/booking/customer/v1/notifications";
    public const string Driver = "/api/dispatch/driver/v1/notifications";

    // 服务端调用客户端的方法名。
    public const string ClientMethod = "notificationReceived";

    private const string NegotiateSuffix = "/negotiate";

    private static readonly string[] Paths = [Staff, Customer, Driver];

    public static bool IsHubPath(string? path) =>
        !string.IsNullOrEmpty(path) && Paths.Any(hub => IsHub(path, hub));

    private static bool IsHub(string path, string hub) =>
        string.Equals(path.TrimEnd('/'), hub, StringComparison.OrdinalIgnoreCase)
        || string.Equals(path.TrimEnd('/'), hub + NegotiateSuffix, StringComparison.OrdinalIgnoreCase);
}
