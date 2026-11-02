using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 坐标的局部更新对象；未出现的键保持原值，不接受显式 null。
/// </summary>
/// <param name="Latitude">纬度，-90 至 90；未传保持原值，不接受 null。</param>
/// <param name="Longitude">经度，-180 至 180；未传保持原值，不接受 null。</param>
public sealed record CoordinatesPatchRequest(Optional<double?> Latitude, Optional<double?> Longitude);
