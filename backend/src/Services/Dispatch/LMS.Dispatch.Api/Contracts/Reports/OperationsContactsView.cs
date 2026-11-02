namespace LMS.Dispatch.Api.Contracts.Reports;

/// <summary>运营联系渠道。</summary>
/// <param name="Name">渠道名称。</param>
/// <param name="Channel">渠道类型，目前为 PHONE。</param>
/// <param name="Value">联系值（例如电话号码）。</param>
public sealed record OperationsContactItemView(string Name, string Channel, string Value);

/// <summary>
/// 司机所属物业的运营联系资料。isAvailable 为 false 表示该物业没有配置可信联系渠道，不会回退成司机个人紧急联系人。
/// 查询到联系资料不表示拨号已接通或运营已受理。
/// </summary>
/// <param name="PropertyId">物业代码。</param>
/// <param name="IsAvailable">是否有可用的运营联系渠道。</param>
/// <param name="Contacts">联系渠道，不可用时为空数组。</param>
public sealed record OperationsContactsView(string PropertyId, bool IsAvailable, IReadOnlyList<OperationsContactItemView> Contacts);
