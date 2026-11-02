namespace LMS.Platform.Domain.Locations;

// 地理坐标，单位为度。坐标参考系待确认（Q02），当前只校验数值范围。
public sealed record Coordinates(double Latitude, double Longitude);
