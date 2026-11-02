namespace LMS.Booking.Domain.TripLocations;

// 行驶距离：把时间窗内按序号排列的相邻轨迹点逐段按球面距离相加。窗口内少于两个点时没有可计算的距离，返回 null，
// 不以 0 冒充“没有行驶”。坐标为 GCJ-02，在城市范围内与 WGS-84 的偏移对分段距离的影响可以忽略。
public static class TripTrackDistance
{
    private const double EarthRadiusMeters = 6_371_008.8;

    public static double? MetersWithin(IReadOnlyList<LocationPoint> points, DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from is null || to is null)
        {
            return null;
        }

        var window = points
            .Where(point => point.RecordedAt >= from && point.RecordedAt <= to)
            .OrderBy(point => point.Seq)
            .ToList();

        return window.Count < 2 ? null : window.Zip(window.Skip(1), Between).Sum();
    }

    private static double Between(LocationPoint first, LocationPoint second)
    {
        var latitude1 = ToRadians(first.Latitude);
        var latitude2 = ToRadians(second.Latitude);
        var deltaLatitude = latitude2 - latitude1;
        var deltaLongitude = ToRadians(second.Longitude - first.Longitude);
        var haversine = Math.Pow(Math.Sin(deltaLatitude / 2), 2)
            + Math.Cos(latitude1) * Math.Cos(latitude2) * Math.Pow(Math.Sin(deltaLongitude / 2), 2);

        return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(haversine)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
