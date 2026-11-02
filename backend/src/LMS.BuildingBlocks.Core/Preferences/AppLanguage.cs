namespace LMS.BuildingBlocks.Core.Preferences;

// 司机 / 客户的界面与通知语言偏好。对外字面值为 EN / ZH_HANT / ZH_HANS。
// 司机主档的 languages 表示司机会讲的语言，不是界面语言，两者不互相覆盖。
public enum AppLanguage
{
    En,
    ZhHant,
    ZhHans
}
