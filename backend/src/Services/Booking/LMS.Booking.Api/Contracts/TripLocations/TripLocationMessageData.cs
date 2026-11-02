namespace LMS.Booking.Api.Contracts.TripLocations;

/// <summary>
/// 位置事件。
/// </summary>
/// <param name="SchemaVersion">消息结构版本，当前为 1；客户端忽略不认识的版本。</param>
/// <param name="EventType">TRIP_LOCATION_UPDATED（位置更新）/ TRIP_LOCATION_ENDED（共享结束，location 为 null）。</param>
/// <param name="TripId">行程标识。</param>
/// <param name="Location">最新位置；共享结束时为 null。</param>
public sealed record TripLocationMessageData(int SchemaVersion, string EventType, string TripId, TripLocationView? Location);
