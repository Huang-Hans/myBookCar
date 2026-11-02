using LMS.Booking.Domain.Guests;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Guests.Models;

// 档案聚合（HA-06）：宾客读模型 + 黑名单状态 + 最近行程。
public sealed record GuestProfileBundle(
    GuestProfile Guest,
    IReadOnlyList<GuestRestriction> BlacklistStatus,
    IReadOnlyList<Trip> RecentTrips);

public sealed record GuestRestriction(bool Listed, string? Reason);

// 资格判定结果（HA-02）：等级、最低可参与等级、游玩核验、可用车型与积分（引用展示）。
public sealed record GuestQualificationCheck(
    string Tier,
    string? TierMinimum,
    bool PlayVerified,
    int PlayWindowDays,
    IReadOnlyList<string> EntitledVehicleClasses,
    long? PointsAvailable,
    bool Qualified);
