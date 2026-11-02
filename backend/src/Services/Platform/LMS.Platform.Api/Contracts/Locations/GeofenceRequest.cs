namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 地点的地理围栏：以该地点坐标为圆心、按半径判定进出。
/// </summary>
/// <param name="IsEnabled">是否启用围栏，Enabled/Disabled 映射为 true/false，必填。</param>
/// <param name="RadiusMeters">围栏半径（米，10 至 5000），必填；圆心取同记录坐标，不使用多边形。</param>
public sealed record GeofenceRequest(bool? IsEnabled, double? RadiusMeters);
