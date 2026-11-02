namespace LMS.Dispatch.Application.Reports.Models;

// 司机所属物业的运营联系资料。IsAvailable 为 false 表示该物业没有配置可信联系渠道（不会回退成司机个人紧急联系人）。
public sealed record OperationsContacts(string PropertyId, bool IsAvailable, IReadOnlyList<OperationsContact> Contacts);
