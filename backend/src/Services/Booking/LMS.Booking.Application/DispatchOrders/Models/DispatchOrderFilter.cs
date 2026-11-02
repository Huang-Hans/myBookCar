using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DispatchOrders.Models;

// 派单队列的筛选条件。IncludeTripIds / ExcludeTripIds 由 Dispatch 按邀请状态计算后传入，只对订单本身做集合限定；
// 为 null 表示不限定。
public sealed record DispatchOrderFilter(
    string? PropertyId,
    TripChannel? Channel,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string Search,
    DispatchOrderGroup Group,
    IReadOnlyList<string>? IncludeTripIds,
    IReadOnlyList<string>? ExcludeTripIds);
