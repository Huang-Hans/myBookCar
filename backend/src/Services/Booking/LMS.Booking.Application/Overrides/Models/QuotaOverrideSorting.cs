namespace LMS.Booking.Application.Overrides.Models;

public static class QuotaOverrideSorting
{
    public static IReadOnlyList<string> Fields { get; } =
        ["propertyId", "guestId", "additionalDailyEntitlements", "justification", "status", "requestedBy", "requestedAt", "overrideId"];

    public const string DefaultSort = "requestedAt,overrideId";
    public const string StableField = "overrideId";
}
