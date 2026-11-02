namespace LMS.Booking.Api.Contracts.TripLocations;

/// <summary>
/// 推送与快照中的车辆位置（GCJ-02）。不含精度、接收时间与司机标识。
/// </summary>
/// <param name="Latitude">纬度。</param>
/// <param name="Longitude">经度。</param>
/// <param name="HeadingDegrees">方向；司机端未上报时为 null。</param>
/// <param name="SpeedKmh">速度；司机端未上报时为 null。</param>
/// <param name="Seq">序号；客户端丢弃不大于已显示序号的位置。</param>
/// <param name="RecordedAt">设备定位时间，UTC DateTime，序列化为以 Z 结尾的 ISO 8601。</param>
public sealed record TripLocationView(
    double Latitude,
    double Longitude,
    double? HeadingDegrees,
    double? SpeedKmh,
    long Seq,
    DateTime RecordedAt);
