namespace LMS.Booking.Api.Contracts.DispatchOrders;

/// <summary>
/// 计数请求正文：已确认订单中处于各邀请状态的订单集合，用来在已确认订单内细分计数。
/// </summary>
/// <param name="AwaitingTripIds">存在未过期待响应邀请的订单。</param>
/// <param name="ProcessingTripIds">邀请已被接受、等待权威指派的订单。</param>
/// <param name="RedispatchTripIds">最近一轮邀请已关闭且需要重派的订单。</param>
public sealed record DispatchOrderCountRequest(
    List<string?>? AwaitingTripIds,
    List<string?>? ProcessingTripIds,
    List<string?>? RedispatchTripIds);
