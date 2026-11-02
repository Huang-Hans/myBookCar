namespace LMS.Dispatch.Api.Contracts.Offers;

/// <summary>
/// 创建派单邀请的请求体（服务间 / 调度来源）。行程摘要由 Booking 权威读取，不接受调用方自带。
/// </summary>
/// <param name="TripId">待分配行程标识。</param>
/// <param name="DriverId">被邀请的司机标识。</param>
public sealed record CreateDriverOfferRequest(string? TripId, string? DriverId);
