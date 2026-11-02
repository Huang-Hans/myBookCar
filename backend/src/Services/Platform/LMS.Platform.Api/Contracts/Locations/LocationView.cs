namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 地点视图，映射 common_locations；空值字段省略。
/// </summary>
/// <param name="LocationId">地点标识。</param>
/// <param name="Name">地点名称；暂不提供多语言名称。</param>
/// <param name="Address">地址。</param>
/// <param name="Coordinates">坐标（经纬度）。</param>
/// <param name="Region">区域代码，MACAU_PENINSULA / COTAI / HONG_KONG / AIRPORT。</param>
/// <param name="CategoryCode">分类代码，使用上传配置代码，不是显示名称。</param>
/// <param name="ContactPerson">联系人。</param>
/// <param name="ContactPhone">联系电话。</param>
/// <param name="OperatingHours">营业时间文本。</param>
/// <param name="Geofence">地理围栏（启用状态与半径）。</param>
/// <param name="IsActive">是否启用。</param>
/// <param name="Notes">备注；为空时省略该键。</param>
/// <param name="SortOrder">区域内持久排序位置，不作为普通编辑字段。</param>
/// <param name="Version">乐观锁版本号，配合 X-Expected-Version 头做并发控制。</param>
public sealed record LocationView(
    string LocationId,
    string Name,
    string Address,
    CoordinatesView Coordinates,
    string Region,
    string CategoryCode,
    string ContactPerson,
    string ContactPhone,
    string OperatingHours,
    GeofenceView Geofence,
    bool IsActive,
    string? Notes,
    int SortOrder,
    long Version);
