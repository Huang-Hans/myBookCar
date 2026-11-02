namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 创建地点请求体；仅包含原型表单可编辑字段，未知字段拒绝。
/// </summary>
/// <param name="Name">地点名称，必填且非空白；暂不提供多语言名称。</param>
/// <param name="Address">地址，必填且非空白。</param>
/// <param name="Coordinates">坐标（经纬度），必填。</param>
/// <param name="Region">区域代码，必填，MACAU_PENINSULA / COTAI / HONG_KONG / AIRPORT。</param>
/// <param name="CategoryCode">分类代码，必填；用上传配置代码，不能传显示名称。</param>
/// <param name="ContactPerson">联系人，必填且非空白。</param>
/// <param name="ContactPhone">联系电话，必填且非空白。</param>
/// <param name="OperatingHours">营业时间文本，必填；不定义每周日历。</param>
/// <param name="Geofence">地理围栏（启用状态与半径），必填。</param>
/// <param name="IsActive">是否启用（Active/Inactive），必填。</param>
/// <param name="Notes">备注，可为空。</param>
public sealed record CreateLocationRequest(
    string? Name,
    string? Address,
    CoordinatesRequest? Coordinates,
    string? Region,
    string? CategoryCode,
    string? ContactPerson,
    string? ContactPhone,
    string? OperatingHours,
    GeofenceRequest? Geofence,
    bool? IsActive,
    string? Notes);
