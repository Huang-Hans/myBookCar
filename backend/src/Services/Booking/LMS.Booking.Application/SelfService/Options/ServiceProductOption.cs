namespace LMS.Booking.Application.SelfService.Options;

// 一个自助服务产品（部署配置，Q05 待业务确认）。ServiceType 是履约侧行程类型的显式映射；
// RouteScope 为 LOCAL（起终点同城）或 CROSS_BORDER（起终点分属澳门与香港）。
// 未启用的产品必须给出 UnavailableReason，由接口原样返回，不伪装成可订。
public sealed record ServiceProductOption(
    string Code,
    string Name,
    string Description,
    bool IsEnabled,
    string ServiceType,
    string RouteScope,
    string? UnavailableReason,
    string VehicleClass = "");
