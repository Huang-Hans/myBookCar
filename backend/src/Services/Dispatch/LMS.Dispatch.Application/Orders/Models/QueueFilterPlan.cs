using LMS.Dispatch.Application.Directory;

namespace LMS.Dispatch.Application.Orders.Models;

// 把派单状态筛选翻译成 Booking 能执行的订单状态分组与订单集合限定。
public sealed record QueueFilterPlan(BookingOrderGroup Group, IReadOnlyList<string>? Include, IReadOnlyList<string>? Exclude);
