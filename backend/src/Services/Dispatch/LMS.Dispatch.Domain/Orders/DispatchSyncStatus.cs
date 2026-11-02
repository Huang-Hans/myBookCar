namespace LMS.Dispatch.Domain.Orders;

// 邀请与 Booking 权威指派的一致性。RECOVERING 表示 Booking 已指派而 Dispatch 的邀请仍在处理中，此时以 Booking 的指派为准，且不允许重复派单。
public enum DispatchSyncStatus
{
    InSync,
    Recovering
}
