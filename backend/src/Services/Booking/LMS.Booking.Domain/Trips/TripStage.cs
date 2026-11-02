namespace LMS.Booking.Domain.Trips;

// 对司机与客户展示的细分阶段：八态主状态加上 IN_PROGRESS 内的子阶段。客户的简化状态由此映射，不改写八态状态机。
public enum TripStage
{
    Pending,
    Confirmed,
    DriverAssigned,
    DriverEnRoute,
    ArrivedAtPickup,
    PassengerOnBoard,
    InTransit,
    ArrivedAtDestination,
    Completed,
    Cancelled
}
