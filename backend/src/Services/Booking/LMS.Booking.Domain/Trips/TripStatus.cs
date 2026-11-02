namespace LMS.Booking.Domain.Trips;

// 行程单状态（HA-04 八态）。COMPLETED 与 CANCELLED 为终态，不允许再修改或取消。
public enum TripStatus
{
    Pending,
    Confirmed,
    DriverAssigned,
    DriverEnRoute,
    ArrivedAtPickup,
    InProgress,
    Completed,
    Cancelled
}
