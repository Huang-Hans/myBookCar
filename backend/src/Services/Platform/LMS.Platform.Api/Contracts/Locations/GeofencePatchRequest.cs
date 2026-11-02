using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 地理围栏的局部更新对象；未出现的键保持原值，不接受显式 null。
/// </summary>
/// <param name="IsEnabled">围栏是否启用；未传保持原值，不接受 null。</param>
/// <param name="RadiusMeters">围栏半径（米，10 至 5000）；未传保持原值，不接受 null。</param>
public sealed record GeofencePatchRequest(Optional<bool?> IsEnabled, Optional<double?> RadiusMeters);
