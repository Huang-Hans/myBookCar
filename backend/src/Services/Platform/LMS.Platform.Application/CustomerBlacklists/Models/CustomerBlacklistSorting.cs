namespace LMS.Platform.Application.CustomerBlacklists.Models;

public static class CustomerBlacklistSorting
{
    public static IReadOnlyList<string> Fields { get; } =
        ["customerName", "memberId", "reason", "effectiveDate", "expiryDate", "isActive", "blacklistId"];

    public const string DefaultSort = "customerName,blacklistId";
    public const string StableField = "blacklistId";
}
