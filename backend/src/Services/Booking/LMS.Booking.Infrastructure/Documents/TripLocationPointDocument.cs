using LMS.Booking.Domain.TripLocations;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

// 行驶轨迹中的一个上报点：_id 为 tripId:seq，同一点重传不会重复写入；expiresAt 配合 TTL 索引自动删除。
internal sealed class TripLocationPointDocument
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    [BsonElement("tripId")]
    public string TripId { get; init; } = string.Empty;

    [BsonElement("seq")]
    public long Seq { get; init; }

    [BsonElement("latitude")]
    public double Latitude { get; init; }

    [BsonElement("longitude")]
    public double Longitude { get; init; }

    [BsonElement("accuracyMeters")]
    public double? AccuracyMeters { get; init; }

    [BsonElement("headingDegrees")]
    public double? HeadingDegrees { get; init; }

    [BsonElement("speedKmh")]
    public double? SpeedKmh { get; init; }

    [BsonElement("recordedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime RecordedAt { get; init; }

    [BsonElement("receivedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReceivedAt { get; init; }

    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; init; }

    public static string IdOf(string tripId, long seq) => $"{tripId}:{seq}";

    public static TripLocationPointDocument From(string tripId, LocationPoint point, DateTimeOffset receivedAt, DateTimeOffset expiresAt) => new()
    {
        Id = IdOf(tripId, point.Seq),
        TripId = tripId,
        Seq = point.Seq,
        Latitude = point.Latitude,
        Longitude = point.Longitude,
        AccuracyMeters = point.AccuracyMeters,
        HeadingDegrees = point.HeadingDegrees,
        SpeedKmh = point.SpeedKmh,
        RecordedAt = UtcDateTimes.ToStored(point.RecordedAt),
        ReceivedAt = UtcDateTimes.ToStored(receivedAt),
        ExpiresAt = UtcDateTimes.ToStored(expiresAt)
    };

    public LocationPoint ToDomain() =>
        new(Seq, Latitude, Longitude, AccuracyMeters, HeadingDegrees, SpeedKmh, UtcDateTimes.FromStored(RecordedAt));
}
