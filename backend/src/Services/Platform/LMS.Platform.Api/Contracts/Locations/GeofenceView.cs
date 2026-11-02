namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 地理围栏视图。
/// </summary>
/// <param name="IsEnabled">围栏是否启用。</param>
/// <param name="RadiusMeters">围栏半径（米）。</param>
public sealed record GeofenceView(bool IsEnabled, double RadiusMeters);
