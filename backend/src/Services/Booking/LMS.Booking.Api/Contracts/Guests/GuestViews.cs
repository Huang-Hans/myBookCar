using LMS.Booking.Api.Contracts.Trips;

namespace LMS.Booking.Api.Contracts.Guests;

/// <summary>宾客检索结果行（HA-01）。</summary>
public sealed record GuestSummaryView(
    string GuestId,
    string Name,
    string? RoomNumber,
    string MemberId,
    string Tier,
    bool InHouse,
    string? PropertyId = null);

/// <summary>宾客档案（HA-06）：会员信息、黑名单状态、偏好与最近行程；电话为掩码值。</summary>
public sealed record GuestProfileView(
    string GuestId,
    string Name,
    string Tier,
    string MemberId,
    string? RoomNumber,
    string? PropertyId,
    DateOnly? StayFrom,
    DateOnly? StayTo,
    long? PointsBalance,
    string? PhoneMasked,
    bool InHouse,
    GuestBlacklistStatusView BlacklistStatus,
    GuestPreferencesView Preferences,
    string? SpecialRequests,
    IReadOnlyList<TripView> RecentTrips);

/// <summary>黑名单状态：不存在有效限制时 listed=false。</summary>
public sealed record GuestBlacklistStatusView(bool Listed, string? Reason);

/// <summary>宾客偏好快照（来自读模型）。</summary>
public sealed record GuestPreferencesView(string? Language, string? PreferredVehicleClass, string? ConfirmationChannel);

/// <summary>资格判定结果（HA-02）。</summary>
public sealed record GuestQualificationCheckView(
    string Tier,
    string? TierMinimum,
    bool PlayVerified,
    int PlayWindowDays,
    IReadOnlyList<string> VehicleEntitlements,
    long? PointsAvailable,
    bool Qualified);
