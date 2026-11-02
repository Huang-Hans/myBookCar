using LMS.Booking.Domain.TripLocations;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

// 行程最新位置：_id 即 tripId，每个行程一条；expiresAt 配合 TTL 索引自动删除。
internal sealed class TripLiveLocationDocument
{
    [BsonId]
    public string TripId { get; init; } = string.Empty;

    [BsonElement("driverId")]
    public string DriverId { get; init; } = string.Empty;

    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

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

    [BsonElement("lastPushedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastPushedAt { get; init; }

    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; init; }

    public TripLiveLocation ToDomain() => new(
        TripId,
        DriverId,
        PropertyId,
        new LocationPoint(Seq, Latitude, Longitude, AccuracyMeters, HeadingDegrees, SpeedKmh, UtcDateTimes.FromStored(RecordedAt)),
        UtcDateTimes.FromStored(ReceivedAt),
        LastPushedAt is null ? null : UtcDateTimes.FromStored(LastPushedAt.Value),
        UtcDateTimes.FromStored(ExpiresAt));
}
