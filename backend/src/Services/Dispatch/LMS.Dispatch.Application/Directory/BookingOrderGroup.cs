namespace LMS.Dispatch.Application.Directory;

// 向 Booking 查询派单队列时的订单状态分组，与 Booking 内部接口的 group 参数一一对应。
public enum BookingOrderGroup
{
    Any,
    PendingConfirmation,
    Confirmed,
    Assigned,
    Completed,
    Cancelled,
    Unassigned
}
