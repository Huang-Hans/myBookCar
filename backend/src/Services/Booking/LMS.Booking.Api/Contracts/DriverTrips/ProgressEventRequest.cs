namespace LMS.Booking.Api.Contracts.DriverTrips;

/// <summary>
/// 提交履约动作的请求体。需要 Idempotency-Key 头；目标状态由服务端状态机决定，客户端不能提交任意状态。
/// </summary>
/// <param name="Action">履约动作：START_EN_ROUTE / ARRIVE_AT_PICKUP / PICK_UP_PASSENGER / START_TRANSIT / ARRIVE_AT_DESTINATION / COMPLETE_TRIP。每一步只有 allowedActions 中给出的下一动作可用。</param>
public sealed record ProgressEventRequest(string? Action);
