using LMS.BuildingBlocks.Core.Preferences;

namespace LMS.Booking.Domain.Customers;

// 客户偏好，键为 guestId：界面 / 通知语言与推送通知总开关。IsPushNotificationsEnabled 只是用户意愿，
// 不代表设备已订阅，也不代表推送已实际投递（投递属第三阶段）。从未保存过的客户读到契约默认（版本 0）。
public sealed record CustomerPreferences(
    string GuestId,
    AppLanguage Language,
    bool IsPushNotificationsEnabled,
    DateTimeOffset? UpdatedAt,
    long Version)
{
    public const AppLanguage DefaultLanguage = AppLanguage.En;

    private const long FirstVersion = 1;

    public static CustomerPreferences Default(string guestId) => new(guestId, DefaultLanguage, false, null, 0);

    public static CustomerPreferences Create(string guestId, AppLanguage language, bool isPushEnabled, DateTimeOffset now) =>
        new(guestId, language, isPushEnabled, now, FirstVersion);

    public CustomerPreferences With(AppLanguage language, bool isPushEnabled, DateTimeOffset now) =>
        this with { Language = language, IsPushNotificationsEnabled = isPushEnabled, UpdatedAt = now };
}
