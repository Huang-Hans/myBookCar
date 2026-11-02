namespace LMS.Dispatch.Api.Contracts.Preferences;

/// <summary>
/// 司机偏好。从未保存过时返回契约默认（language=EN，version=0，不含 updatedAt），不写库。
/// </summary>
/// <param name="Language">界面语言：EN / ZH_HANT / ZH_HANS。与司机主档的 languages（会讲的语言）互不覆盖；选择语言不代表界面翻译已实现。</param>
/// <param name="UpdatedAt">最近保存时间（UTC）；从未保存过时省略。</param>
/// <param name="Version">乐观锁版本号；未保存过为 0。</param>
public sealed record DriverPreferencesView(string Language, DateTimeOffset? UpdatedAt, long Version);
