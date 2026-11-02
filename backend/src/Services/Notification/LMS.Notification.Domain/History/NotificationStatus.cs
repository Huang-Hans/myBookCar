namespace LMS.Notification.Domain.History;

// 投递状态（对外字面值 SENT / DELIVERED / FAILED）。SENT 源于 PM-AD-28，原型筛选目前只显示后两项。
public enum NotificationStatus
{
    Sent,
    Delivered,
    Failed
}
