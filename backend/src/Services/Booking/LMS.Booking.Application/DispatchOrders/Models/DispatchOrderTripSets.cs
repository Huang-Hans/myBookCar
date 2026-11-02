namespace LMS.Booking.Application.DispatchOrders.Models;

// Dispatch 按邀请状态算出的订单集合，用来在已确认订单内细分计数。
public sealed record DispatchOrderTripSets(
    IReadOnlyList<string> Awaiting,
    IReadOnlyList<string> Processing,
    IReadOnlyList<string> Redispatch);
