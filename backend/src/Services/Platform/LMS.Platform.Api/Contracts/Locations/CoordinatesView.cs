namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 地点坐标视图。
/// </summary>
/// <param name="Latitude">纬度。</param>
/// <param name="Longitude">经度。</param>
public sealed record CoordinatesView(double Latitude, double Longitude);
