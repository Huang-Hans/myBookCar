namespace LMS.ApiGateway.Documentation.Sources;

// 网关聚合的下游服务文档。地址是 AppHost 注册的服务名，由服务发现解析，网关不配置固定地址。
// 网关侧路径不能放在 /swagger 下：Swashbuckle 的中间件按 swagger/{documentName}/swagger.json
// 接管这一路径空间，未注册的文档名在那里直接失败，聚合端点不会被执行。
internal static class DownstreamDocuments
{
    public static readonly DownstreamDocument Platform = new(
        "platform", "LMS.Platform.Api", "Platform（平台）", "/openapi/platform.json", "http://platform-api/swagger/v1/swagger.json");

    public static readonly DownstreamDocument Dispatch = new(
        "dispatch", "LMS.Dispatch.Api", "Dispatch（调度）", "/openapi/dispatch.json", "http://dispatch-api/swagger/v1/swagger.json");

    public static readonly DownstreamDocument Booking = new(
        "booking", "LMS.Booking.Api", "Booking（预订）", "/openapi/booking.json", "http://booking-api/swagger/v1/swagger.json");

    public static readonly DownstreamDocument Notification = new(
        "notification", "LMS.Notification.Api", "Notification（通知）", "/openapi/notification.json", "http://notification-api/swagger/v1/swagger.json");

    public static readonly DownstreamDocument Reporting = new(
        "reporting", "LMS.Reporting.Api", "Reporting（报表）", "/openapi/reporting.json", "http://reporting-api/swagger/v1/swagger.json");

    public static IReadOnlyList<DownstreamDocument> All { get; } = [Platform, Dispatch, Booking, Notification, Reporting];
}
