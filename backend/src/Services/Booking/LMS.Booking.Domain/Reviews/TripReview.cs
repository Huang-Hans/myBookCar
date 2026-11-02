namespace LMS.Booking.Domain.Reviews;

// 客户对一次已完成行程的评价，键为 tripId（每个行程至多一条，行程只有一位受益客户）。评分 1～5，评语可空、最多 1000 个字符（按 Unicode 字符计）。
// DriverId 是行程指派的司机快照，不由客户选择；评价不改变行程、积分或司机等级。修改保留创建时间，更新修改时间与版本。
public sealed record TripReview(
    string TripId,
    string GuestId,
    string TripReference,
    string? DriverId,
    int Rating,
    string? Comments,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version)
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int MaxCommentLength = 1000;

    private const long FirstVersion = 1;

    public static TripReview Create(
        string tripId,
        string guestId,
        string tripReference,
        string? driverId,
        int rating,
        string? comments,
        DateTimeOffset now) =>
        new(tripId, guestId, tripReference, driverId, rating, comments, now, now, FirstVersion);

    public TripReview Edit(int rating, string? comments, DateTimeOffset now) =>
        this with { Rating = rating, Comments = comments, UpdatedAt = now };

    public bool HasSameContent(int rating, string? comments) => Rating == rating && string.Equals(Comments, comments, StringComparison.Ordinal);
}
