namespace LMS.Booking.Api.Contracts.DispatchOrders;

/// <summary>
/// 列表请求正文：由 Dispatch 按邀请状态算出的订单集合。两者都省略表示不按集合限定。
/// </summary>
/// <param name="IncludeTripIds">只保留这些订单。</param>
/// <param name="ExcludeTripIds">排除这些订单。</param>
public sealed record DispatchOrderTripIdsRequest(List<string?>? IncludeTripIds, List<string?>? ExcludeTripIds);
