namespace LMS.Notification.Domain.History;

// 投递渠道（对外字面值 EMAIL / SMS / PUSH / IN_APP）。
public enum NotificationChannel
{
    Email,
    Sms,
    Push,
    InApp
}
