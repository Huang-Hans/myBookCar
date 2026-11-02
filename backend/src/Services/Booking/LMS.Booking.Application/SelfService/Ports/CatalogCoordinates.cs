namespace LMS.Booking.Application.SelfService.Ports;

// 目录地点的地图坐标，单位为度（纬度在前）。数值原样来自 Platform 地点目录 coordinates，不做坐标系转换，也不由地址推算；
// 目录的坐标参考系尚未确认（数据模型 Q02），这里只保证数值范围合法。
public sealed record CatalogCoordinates(double Latitude, double Longitude)
{
    private const double MaxLatitude = 90;
    private const double MaxLongitude = 180;

    // 有限数且纬度 ±90、经度 ±180 时返回坐标，否则返回 null：不修正、不猜测。
    public static CatalogCoordinates? TryCreate(double latitude, double longitude) =>
        double.IsFinite(latitude) && double.IsFinite(longitude) && Math.Abs(latitude) <= MaxLatitude && Math.Abs(longitude) <= MaxLongitude
            ? new CatalogCoordinates(latitude, longitude)
            : null;
}
