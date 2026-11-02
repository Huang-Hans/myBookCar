namespace LMS.Booking.Api.Contracts.TripLocations;

/// <summary>
/// 客户端方法 tripLocationUpdated 收到的消息，外层与 notificationReceived 一致。
/// </summary>
/// <param name="Code">固定 20000。</param>
/// <param name="Message">固定 success。</param>
/// <param name="Data">位置事件。</param>
/// <param name="TraceId">追踪标识。</param>
public sealed record TripLocationMessage(int Code, string Message, TripLocationMessageData Data, string TraceId);
