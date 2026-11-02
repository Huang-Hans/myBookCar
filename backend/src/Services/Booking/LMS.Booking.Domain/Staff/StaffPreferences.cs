using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Domain.Staff;

// 员工默认通知渠道（Console Settings）。SMS 之外的首期为站内/客房留言展示值。
public enum StaffNotificationChannel
{
    Sms,
    InRoomMessage
}

// 员工控制台偏好（本人自助，按 userId 定位）：工作站、默认渠道、确认语言、通知类型开关、地点收藏与最近使用。
public sealed record StaffPreferences(
    string UserId,
    string Station,
    StaffNotificationChannel DefaultNotificationChannel,
    string ConfirmationLanguage,
    IReadOnlyList<string> EnabledNotificationTypes,
    IReadOnlyList<string> FavoriteLocationIds,
    IReadOnlyList<string> RecentLocationIds)
{
    public const int MaxFavorites = 20;
    public const int MaxRecent = 6;

    public static IReadOnlyList<string> AllNotificationTypes { get; } =
    [
        "BOOKING_CONFIRMED", "DRIVER_ASSIGNED", "DRIVER_ARRIVED", "TRIP_COMPLETED", "APPROVAL_REQUIRED", "SYSTEM_ALERT"
    ];

    public static StaffPreferences Defaults(string userId) => new(
        userId, string.Empty, StaffNotificationChannel.Sms, "EN", AllNotificationTypes, [], []);

    public static IReadOnlyList<FieldIssue> Validate(
        string station,
        IReadOnlyList<string> enabledNotificationTypes,
        IReadOnlyList<string> favoriteLocationIds,
        IReadOnlyList<string> recentLocationIds)
    {
        var issues = new List<FieldIssue>();

        if (station.Length is < 1 or > 40)
        {
            issues.Add(new FieldIssue("station", "Must be between 1 and 40 characters"));
        }

        var known = AllNotificationTypes.ToHashSet(StringComparer.Ordinal);
        if (enabledNotificationTypes.Any(type => !known.Contains(type))
            || enabledNotificationTypes.Distinct(StringComparer.Ordinal).Count() != enabledNotificationTypes.Count)
        {
            issues.Add(new FieldIssue("enabledNotificationTypes", "Must contain known notification types without duplicates"));
        }

        if (favoriteLocationIds.Count > MaxFavorites || favoriteLocationIds.Distinct(StringComparer.Ordinal).Count() != favoriteLocationIds.Count)
        {
            issues.Add(new FieldIssue("favoriteLocationIds", $"Must contain at most {MaxFavorites} unique location ids"));
        }

        if (recentLocationIds.Count > MaxRecent || recentLocationIds.Distinct(StringComparer.Ordinal).Count() != recentLocationIds.Count)
        {
            issues.Add(new FieldIssue("recentLocationIds", $"Must contain at most {MaxRecent} unique location ids"));
        }

        return issues;
    }
}
