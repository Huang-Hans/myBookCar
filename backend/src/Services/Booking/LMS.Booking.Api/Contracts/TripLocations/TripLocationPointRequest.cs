namespace LMS.Booking.Api.Contracts.TripLocations;

/// <summary>
/// 一个定位点。
/// </summary>
/// <param name="Seq">同一行程内单调递增的序号（≥ 1），用于去重与防止旧点覆盖新点。</param>
/// <param name="Latitude">纬度，-90～90。</param>
/// <param name="Longitude">经度，-180～180。</param>
/// <param name="AccuracyMeters">设备给出的精度（米，≥ 0）；可省略。</param>
/// <param name="HeadingDegrees">方向（0～360，正北为 0）；可省略。</param>
/// <param name="SpeedKmh">速度（公里/小时，≥ 0）；可省略。</param>
/// <param name="RecordedAt">设备定位时间，ISO 8601 UTC 并以 Z 结尾；不是发送时间。</param>
public sealed record TripLocationPointRequest(
    long? Seq,
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    double? HeadingDegrees,
    double? SpeedKmh,
    DateTimeOffset? RecordedAt);
