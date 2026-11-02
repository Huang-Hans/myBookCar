using LMS.Booking.Domain.Common;

namespace LMS.Booking.Domain.Overrides;

// 额外每日权益申请。批准或拒绝只记录经理的决定，不修改任何物业限额（实际生效语义待 Q05）。
public sealed record QuotaOverride(
    string OverrideId,
    string PropertyId,
    string GuestId,
    int AdditionalDailyEntitlements,
    string Justification,
    OverrideStatus Status,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    OverrideDecision? Decision,
    long Version)
{
    public const int MinAdditionalEntitlements = 1;
    public const int MaxAdditionalEntitlements = 20;

    private const string IdPrefix = "override_";
    private const long FirstVersion = 1;

    public static QuotaOverride Create(
        string propertyId,
        string guestId,
        int additionalDailyEntitlements,
        string justification,
        string requestedBy,
        DateTimeOffset requestedAt) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            propertyId, guestId, additionalDailyEntitlements, justification, OverrideStatus.ManagerApprovalRequired,
            requestedBy, requestedAt, null, FirstVersion);

    public QuotaOverride Decide(ReviewDecision decision, string decidedBy, DateTimeOffset decidedAt) =>
        this with
        {
            Status = decision == ReviewDecision.Approved ? OverrideStatus.Approved : OverrideStatus.Rejected,
            Decision = new OverrideDecision(decision, decidedBy, decidedAt)
        };
}
