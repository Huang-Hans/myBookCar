namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 系统参数视图，提供门户语言、时区等展示与环境参数。
/// </summary>
/// <param name="Language">当前语言代码（原型 English 对应 en）。</param>
/// <param name="TimeZone">时区标识（原型 Asia/Macau）。</param>
/// <param name="DateFormat">展示日期格式（原型 DD MMM YYYY），不改变 API 的 UTC 时间格式。</param>
/// <param name="SessionTimeoutMinutes">会话超时分钟数（>0）。</param>
/// <param name="PasswordPolicyReference">企业 IdP 密码政策引用。</param>
/// <param name="PointsRedemptionMode">积分兑换模式，当前仅 REFERENCE_ONLY。</param>
/// <param name="PointsExpiryReference">外部会员系统的积分失效规则引用。</param>
public sealed record SystemParametersView(
    string Language,
    string TimeZone,
    string DateFormat,
    int SessionTimeoutMinutes,
    string PasswordPolicyReference,
    string PointsRedemptionMode,
    string PointsExpiryReference);
