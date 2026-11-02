using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Booking.Api.Contracts.ConsoleNotifications;

/// <summary>站内通知行（HA-11）。type 为 6 个事件字面值之一；targetType（TRIP / QUALIFICATION）与 targetId 是点击后的跳转目标，存量通知省略。</summary>
public sealed record ConsoleNotificationView(
    string NotificationId,
    string Type,
    string Summary,
    string? TripReference,
    DateTimeOffset OccurredAt,
    bool IsRead,
    DateTimeOffset? ReadAt,
    string? TargetType = null,
    string? TargetId = null);

/// <summary>通知偏好（本人自助）：6 类事件开关，全开为默认。</summary>
public sealed record NotificationPreferencesRequest(IReadOnlyList<string>? EnabledNotificationTypes);

/// <summary>工作站设定（本人自助）：站点、默认通知渠道与宾客确认语言。</summary>
public sealed record ConsoleSettingsRequest(string? Station, string? DefaultNotificationChannel, string? ConfirmationLanguage);

/// <summary>地点收藏（本人自助）：全量替换，地点必须存在且启用。</summary>
public sealed record LocationFavoritesRequest(IReadOnlyList<string>? FavoriteLocationIds);

/// <summary>偏好回显视图。</summary>
public sealed record StaffPreferencesView(
    string Station,
    string DefaultNotificationChannel,
    string ConfirmationLanguage,
    IReadOnlyList<string> EnabledNotificationTypes,
    IReadOnlyList<string> FavoriteLocationIds,
    IReadOnlyList<string> RecentLocationIds);
