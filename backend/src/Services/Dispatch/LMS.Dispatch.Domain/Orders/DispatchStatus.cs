namespace LMS.Dispatch.Domain.Orders;

// 派单页面状态。订单状态（orderStatus）、派单状态（dispatchStatus）与邀请状态（offer.status）是三个独立的口径，不能混用。
// PENDING_CONFIRMATION 保留为兼容值，不再产生；PENDING 为待派单：订单已确认（含存量 PENDING 订单）且没有有效邀请；是否可派由 canDispatch 判断；
// AWAITING_RESPONSE 存在未过期待响应邀请；
// PROCESSING 邀请已被接受、等待权威指派；DRIVER_ASSIGNED Booking 已指派司机；NEEDS_REDISPATCH 最近一轮邀请已关闭且订单仍可分配；
// COMPLETED 订单已完成；CANCELLED 订单已取消，两者均不可派单。
public enum DispatchStatus
{
    PendingConfirmation,
    Pending,
    AwaitingResponse,
    Processing,
    DriverAssigned,
    NeedsRedispatch,
    Completed,
    Cancelled
}
