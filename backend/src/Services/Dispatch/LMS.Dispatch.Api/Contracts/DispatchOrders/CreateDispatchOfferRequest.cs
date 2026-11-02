namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

/// <summary>
/// 发送邀请的请求体。需要 Idempotency-Key 头；X-Expected-Version 可选，提供时必须等于订单当前的派单版本（最近一轮邀请的轮次）。
/// </summary>
/// <param name="DriverId">被邀请的司机标识，来自候选查询的 driverId。</param>
public sealed record CreateDispatchOfferRequest(string? DriverId);
