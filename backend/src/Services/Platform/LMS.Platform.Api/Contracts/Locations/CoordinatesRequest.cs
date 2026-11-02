namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 创建地点时提交的坐标（经纬度）。
/// </summary>
/// <param name="Latitude">纬度，-90 至 90 的有限数值，必填。</param>
/// <param name="Longitude">经度，-180 至 180 的有限数值，必填。</param>
public sealed record CoordinatesRequest(double? Latitude, double? Longitude);
