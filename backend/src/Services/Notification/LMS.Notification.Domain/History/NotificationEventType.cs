namespace LMS.Notification.Domain.History;

// 触发通知的业务事件（对外字面值与上传的事件模板一致）。
public enum NotificationEventType
{
    BookingConfirmation,
    TripReminder,
    DriverAssigned,
    TripStarted,
    BookingCancelled
}
