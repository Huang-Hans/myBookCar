namespace LMS.Dispatch.Application.Directory;

// 按邀请状态算出的已确认订单集合，用于 Booking 侧细分计数。
public sealed record BookingOrderTripSets(
    IReadOnlyList<string> Awaiting,
    IReadOnlyList<string> Processing,
    IReadOnlyList<string> Redispatch);
