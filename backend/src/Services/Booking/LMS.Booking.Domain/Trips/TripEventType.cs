namespace LMS.Booking.Domain.Trips;

// 履约轨迹事件类型。
public enum TripEventType
{
    DriverAssigned,
    StartedEnRoute,
    ArrivedAtPickup,
    PassengerBoarded,
    InTransit,
    ArrivedAtDestination,
    Completed,
    Cancelled
}
