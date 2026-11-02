using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Booking.Domain.Reviews;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

// 行程评价：_id 即 tripId，每个行程至多一条，并发的首次评价由主键唯一性裁决。
internal sealed class TripReviewDocument
{
    [BsonId]
    public string TripId { get; init; } = string.Empty;

    [BsonElement("guestId")]
    public string GuestId { get; init; } = string.Empty;

    [BsonElement("tripReference")]
    public string TripReference { get; init; } = string.Empty;

    [BsonElement("driverId")]
    public string? DriverId { get; init; }

    [BsonElement("rating")]
    public int Rating { get; init; }

    [BsonElement("comments")]
    public string? Comments { get; init; }

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; }

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static TripReviewDocument FromDomain(TripReview review) => new()
    {
        TripId = review.TripId,
        GuestId = review.GuestId,
        TripReference = review.TripReference,
        DriverId = review.DriverId,
        Rating = review.Rating,
        Comments = review.Comments,
        CreatedAt = UtcDateTimes.ToStored(review.CreatedAt),
        UpdatedAt = UtcDateTimes.ToStored(review.UpdatedAt),
        Version = review.Version
    };

    public TripReview ToDomain() => new(
        TripId, GuestId, TripReference, DriverId, Rating, Comments,
        UtcDateTimes.FromStored(CreatedAt), UtcDateTimes.FromStored(UpdatedAt), Version);
}
