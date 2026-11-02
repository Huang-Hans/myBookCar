namespace LMS.Booking.Application.SelfService.Ports;

// 单个目录地点的客户可见信息（含坐标与是否启用）。不含联系人、联系电话、围栏等员工管理字段。
// Coordinates 为 null 表示目录没有返回可用坐标。
public sealed record CatalogLocationProfile(
    string LocationId,
    string Name,
    string Address,
    string Region,
    string CategoryCode,
    string OperatingHours,
    bool IsActive,
    CatalogCoordinates? Coordinates);
