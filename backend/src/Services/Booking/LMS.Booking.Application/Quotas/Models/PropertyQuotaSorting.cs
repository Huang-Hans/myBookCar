namespace LMS.Booking.Application.Quotas.Models;

public static class PropertyQuotaSorting
{
    public static IReadOnlyList<string> Fields { get; } =
        ["propertyId", "dailyQuota", "weeklyQuota", "monthlyQuota", "dailyUsed", "weeklyUsed", "monthlyUsed", "exhaustionPolicy"];

    public const string DefaultSort = "propertyId";
    public const string StableField = "propertyId";
}
