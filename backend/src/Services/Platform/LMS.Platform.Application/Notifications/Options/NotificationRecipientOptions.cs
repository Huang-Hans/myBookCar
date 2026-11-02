namespace LMS.Platform.Application.Notifications.Options;

// 实时通知（客户下单）的客服收件人规则。允许的角色集合是业务决定（待确认），不提供默认值：
// 不以“全部员工”或“全部管理员”代替业务收件人。Roles 为空时收件人查询返回“暂不可用”，而不是零收件人，
// 待处理的客服下单通知因此保留重试，不会被当作已完成。角色使用 PortalRole 的对外字面值（如 DISPATCHER）。
public sealed class NotificationRecipientOptions
{
    public const string SectionName = "NotificationRecipients";

    public string[] Roles { get; init; } = [];

    // 单次查询允许返回的收件人上限；超过时整体失败，不静默截断目录。
    public int MaxRecipients { get; init; } = 1000;
}
