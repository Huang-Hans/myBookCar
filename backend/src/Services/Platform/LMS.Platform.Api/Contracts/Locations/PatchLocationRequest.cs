using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 局部更新地点请求体；至少传一项，未出现的键保持原值，嵌入对象按其 Patch 类型合并。
/// </summary>
/// <param name="Name">地点名称；未传保持原值，不接受 null 或空白。</param>
/// <param name="Address">地址；未传保持原值，不接受 null 或空白。</param>
/// <param name="Coordinates">坐标局部更新；未传保持原值，不接受显式 null。</param>
/// <param name="Region">区域代码；未传保持原值，取值须为合法区域字面值，不接受 null。</param>
/// <param name="CategoryCode">分类代码；未传保持原值，不接受 null 或空白。</param>
/// <param name="ContactPerson">联系人；未传保持原值，不接受 null 或空白。</param>
/// <param name="ContactPhone">联系电话；未传保持原值，不接受 null 或空白。</param>
/// <param name="OperatingHours">营业时间文本；未传保持原值，不接受 null 或空白。</param>
/// <param name="Geofence">地理围栏局部更新；未传保持原值，不接受显式 null。</param>
/// <param name="IsActive">是否启用；未传保持原值，不接受 null。</param>
/// <param name="Notes">备注；未传保持原值，显式传 null 或空白表示清空。</param>
public sealed record PatchLocationRequest(
    Optional<string?> Name,
    Optional<string?> Address,
    Optional<CoordinatesPatchRequest?> Coordinates,
    Optional<string?> Region,
    Optional<string?> CategoryCode,
    Optional<string?> ContactPerson,
    Optional<string?> ContactPhone,
    Optional<string?> OperatingHours,
    Optional<GeofencePatchRequest?> Geofence,
    Optional<bool?> IsActive,
    Optional<string?> Notes);
