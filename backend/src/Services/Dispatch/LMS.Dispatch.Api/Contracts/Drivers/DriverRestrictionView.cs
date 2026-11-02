namespace LMS.Dispatch.Api.Contracts.Drivers;

/// <summary>
/// 司机黑名单展示信息；由停用或暂停动作产生，不增加第二个司机黑名单集合，为空时省略该键。
/// </summary>
/// <param name="Reason">黑名单展示原因：停用为 Deactivated by Operations，暂停为 Suspended by Operations；无值时为 null。</param>
/// <param name="EffectiveDate">停用或暂停生效业务日期，YYYY-MM-DD；无值时为 null。</param>
public sealed record DriverRestrictionView(string? Reason, string? EffectiveDate);
