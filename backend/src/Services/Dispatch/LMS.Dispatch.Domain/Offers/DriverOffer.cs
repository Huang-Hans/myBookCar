namespace LMS.Dispatch.Domain.Offers;

// 一轮派单邀请。OfferId 每轮唯一，不与行程标识混用；同一行程多轮邀请对应多个 OfferId。
// OfferId 同时作为 Booking 指派的稳定操作标识：重试同一邀请不会产生第二笔指派。
// Round 是同一行程的第几轮邀请（从 1 起；早于轮次记录的存量邀请为 0）；DriverName 与 CreatedBy 是创建时的快照（存量邀请为空）。
public sealed record DriverOffer(
    string OfferId,
    string TripId,
    string TripReference,
    string DriverId,
    string PropertyId,
    OfferTripSummary Trip,
    DriverOfferStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt,
    long Version,
    int Round = 0,
    string? DriverName = null,
    string? CreatedBy = null)
{
    public const string IdPrefix = "offer_";

    private const long FirstVersion = 1;

    public static DriverOffer Create(
        string tripId,
        string tripReference,
        string driverId,
        string propertyId,
        OfferTripSummary trip,
        TimeSpan validity,
        DateTimeOffset now,
        int round,
        string driverName,
        string createdBy) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            tripId, tripReference, driverId, propertyId, trip,
            DriverOfferStatus.Pending, now.Add(validity), now, null, FirstVersion, round, driverName, createdBy);

    public bool IsExpiredAt(DateTimeOffset now) => Status == DriverOfferStatus.Pending && now >= ExpiresAt;

    // 仍在占用行程与司机的邀请：待响应或处理中。同一行程同时只能有一轮、同一司机同时只能持有一张，由存储的唯一约束保证。
    public bool IsOpen => Status is DriverOfferStatus.Pending or DriverOfferStatus.Accepting;

    public DriverOffer Accepting() => this with { Status = DriverOfferStatus.Accepting };

    public DriverOffer Accepted(DateTimeOffset now) => Close(DriverOfferStatus.Accepted, now);

    public DriverOffer Declined(DateTimeOffset now) => Close(DriverOfferStatus.Declined, now);

    public DriverOffer Expired(DateTimeOffset now) => Close(DriverOfferStatus.Expired, now);

    public DriverOffer Superseded(DateTimeOffset now) => Close(DriverOfferStatus.Superseded, now);

    private DriverOffer Close(DriverOfferStatus status, DateTimeOffset now) => this with { Status = status, RespondedAt = now };
}
