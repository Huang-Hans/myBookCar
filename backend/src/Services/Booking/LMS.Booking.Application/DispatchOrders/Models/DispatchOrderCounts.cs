namespace LMS.Booking.Application.DispatchOrders.Models;

// 同一筛选范围内按订单状态分组的计数，以及待指派订单（候补 / 审批中或已确认）落在各邀请集合里的数量；字段名保持既有契约。
public sealed record DispatchOrderCounts(
    long PendingConfirmation,
    long Confirmed,
    long Assigned,
    long Completed,
    long Cancelled,
    long ConfirmedAwaiting,
    long ConfirmedProcessing,
    long ConfirmedRedispatch);
