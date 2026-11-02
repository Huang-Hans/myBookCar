using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Domain.Orders;

// 派单状态由 Booking 的订单状态与 Dispatch 最近一轮邀请共同推导，不单独存储，因此不会与任一权威来源产生分歧。
public static class DispatchStatusRules
{
    public static DispatchStatus Resolve(OrderStatus order, DriverOffer? latestOffer, DateTimeOffset now) => order switch
    {
        OrderStatus.Pending or OrderStatus.Confirmed => ResolveAssignable(latestOffer, now),
        OrderStatus.Completed => DispatchStatus.Completed,
        OrderStatus.Cancelled => DispatchStatus.Cancelled,
        _ => DispatchStatus.DriverAssigned
    };

    public static bool CanDispatch(DispatchStatus status) => status is DispatchStatus.Pending or DispatchStatus.NeedsRedispatch;

    public static DispatchBlockedReason? BlockedReasonOf(DispatchStatus status) => status switch
    {
        DispatchStatus.PendingConfirmation => DispatchBlockedReason.PendingConfirmation,
        DispatchStatus.AwaitingResponse => DispatchBlockedReason.OfferPending,
        DispatchStatus.Processing => DispatchBlockedReason.OfferProcessing,
        DispatchStatus.DriverAssigned => DispatchBlockedReason.DriverAssigned,
        DispatchStatus.Completed => DispatchBlockedReason.OrderCompleted,
        DispatchStatus.Cancelled => DispatchBlockedReason.OrderCancelled,
        _ => null
    };

    public static DispatchSyncStatus SyncOf(DispatchStatus status, DriverOffer? latestOffer) =>
        status == DispatchStatus.DriverAssigned && latestOffer is { Status: DriverOfferStatus.Accepting }
            ? DispatchSyncStatus.Recovering
            : DispatchSyncStatus.InSync;

    // 可分配订单（已确认与存量 PENDING）：最近一轮邀请决定细分状态；有效邀请阻止重复派单。
    private static DispatchStatus ResolveAssignable(DriverOffer? latest, DateTimeOffset now) => latest switch
    {
        null => DispatchStatus.Pending,
        { Status: DriverOfferStatus.Accepting } => DispatchStatus.Processing,
        { Status: DriverOfferStatus.Pending } => latest.IsExpiredAt(now) ? DispatchStatus.NeedsRedispatch : DispatchStatus.AwaitingResponse,
        { Status: DriverOfferStatus.Accepted } => DispatchStatus.Pending,
        _ => DispatchStatus.NeedsRedispatch
    };
}
