namespace LMS.Platform.Application.Configuration.Models;

// 通知事件。对外字面值为 BOOKING_CONFIRMATION / TRIP_REMINDER / DRIVER_ASSIGNED / TRIP_STARTED / BOOKING_CANCELLED。
public enum NotificationEventType
{
    BookingConfirmation,
    TripReminder,
    DriverAssigned,
    TripStarted,
    BookingCancelled
}
