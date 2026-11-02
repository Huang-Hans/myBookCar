namespace LMS.Booking.Application.DispatchOrders.Models;

// 派单队列按订单状态分组的筛选。Any 不限状态；PendingConfirmation 为候补 / 待确认；Confirmed 为已确认且尚未指派；
// Assigned 为已指派到进行中；Completed 为已完成；Cancelled 为已取消。
// Unassigned 合并候补 / 审批中与已确认订单，邀请相关的细分（等待响应、处理中、需重派）由 Dispatch 在其中细分。
public enum DispatchOrderGroup
{
    Any,
    PendingConfirmation,
    Confirmed,
    Assigned,
    Completed,
    Cancelled,
    Unassigned
}
