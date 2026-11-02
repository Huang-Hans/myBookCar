namespace LMS.Booking.Domain.TripLocations;

// 司机端上报的一个定位点（GCJ-02）。Seq 是设备在同一行程内单调递增的序号，用于去重与防止旧点覆盖新点；
// RecordedAt 是设备定位时间，不是服务端接收时间。
public sealed record LocationPoint(
    long Seq,
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    double? HeadingDegrees,
    double? SpeedKmh,
    DateTimeOffset RecordedAt);
