namespace LMS.Platform.Domain.Locations;

// 圆形地理围栏：圆心取同一地点的坐标，半径单位为米。
public sealed record Geofence(bool IsEnabled, double RadiusMeters);
