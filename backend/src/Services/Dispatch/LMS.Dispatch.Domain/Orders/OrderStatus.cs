namespace LMS.Dispatch.Domain.Orders;

// Booking 订单状态（八态）在 Dispatch 的只读镜像，仅用于推导派单状态；订单状态的权威与迁移规则属于 Booking。
public enum OrderStatus
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
