namespace LMS.Booking.Domain.TripLocations;

// 行驶轨迹的保留与返回规则：保存共享窗口内的全部上报点，按保留期自动删除；
// 一次返回的点数有上限，超出时等间隔抽样并保留首尾两点，保证轨迹起止位置准确。
public static class TripLocationTrack
{
    // 每个点在写入后保留的时长，供事后核对与投诉处理。
    public static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    // 一次返回的最大点数（每 5 秒一点约 2.8 小时）。
    public const int MaxPoints = 2000;

    public static bool NeedsSampling(IReadOnlyList<LocationPoint> points) => points.Count > MaxPoints;

    // 点数不超过上限时原样返回；否则在首尾之间等间隔取 MaxPoints 个点。
    public static IReadOnlyList<LocationPoint> Sample(IReadOnlyList<LocationPoint> points)
    {
        if (!NeedsSampling(points))
        {
            return points;
        }

        var lastIndex = points.Count - 1;

        return Enumerable.Range(0, MaxPoints)
            .Select(position => points[(int)((long)position * lastIndex / (MaxPoints - 1))])
            .ToList();
    }
}
