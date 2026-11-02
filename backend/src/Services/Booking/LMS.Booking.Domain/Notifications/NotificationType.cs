namespace LMS.Booking.Domain.Notifications;

// 站内通知事件类型（HA-11），与控制台原型筛选一致。短信/推送渠道属二期。
public enum NotificationType
{
    BookingConfirmed,
    DriverAssigned,
    DriverArrived,
    TripCompleted,
    ApprovalRequired,
    SystemAlert
}
