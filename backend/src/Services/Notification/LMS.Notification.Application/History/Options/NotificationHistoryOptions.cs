namespace LMS.Notification.Application.History.Options;

// 通知历史的数据来源设置。投递事实来源（Q03/Q11）尚未确定，默认视为未接入：此时读取明确返回“来源不可用”，
// 不返回空列表冒充真实结果。只有投递记录的摄入真正接好之后，才把 DeliverySourceConnected 设为 true，
// 此后没有记录就是真实的空结果。
public sealed class NotificationHistoryOptions
{
    public const string SectionName = "NotificationHistory";

    public bool DeliverySourceConnected { get; init; }
}
