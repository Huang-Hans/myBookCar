using LMS.BuildingBlocks.Core.Preferences;

namespace LMS.Dispatch.Domain.Preferences;

// 司机偏好，键为 driverId。目前只有界面语言；司机主档的 languages 是会讲的语言，两者互不覆盖。
// 未保存过偏好的司机读到契约默认（EN，版本 0，无更新时间），不伪造历史写入。
public sealed record DriverPreferences(string DriverId, AppLanguage Language, DateTimeOffset? UpdatedAt, long Version)
{
    public const AppLanguage DefaultLanguage = AppLanguage.En;

    private const long FirstVersion = 1;

    public static DriverPreferences Default(string driverId) => new(driverId, DefaultLanguage, null, 0);

    public static DriverPreferences Create(string driverId, AppLanguage language, DateTimeOffset now) =>
        new(driverId, language, now, FirstVersion);

    public DriverPreferences WithLanguage(AppLanguage language, DateTimeOffset now) =>
        this with { Language = language, UpdatedAt = now };
}
