namespace LMS.Platform.Api.Contracts.Internal;

/// <summary>
/// 有资格接收通知的员工列表；可为空数组（查询成功但没有符合条件的员工）。
/// </summary>
/// <param name="Recipients">收件人，每项只含 Platform 本地 userId。</param>
public sealed record NotificationRecipientsView(IReadOnlyList<NotificationRecipientView> Recipients);
