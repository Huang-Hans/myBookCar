namespace LMS.Dispatch.Domain.Orders;

// 订单当前不能发送邀请的原因；可派单时没有原因。
public enum DispatchBlockedReason
{
    PendingConfirmation,
    OfferPending,
    OfferProcessing,
    DriverAssigned,
    OrderCancelled,
    OrderCompleted
}
