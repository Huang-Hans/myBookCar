namespace LMS.Booking.Domain.TripLocations;

// 行程的最新位置：每个行程一条，与行程主文档分开存储，避免每几秒一次的写入与履约推进争用行程版本。
// ExpiresAt 到期后由 TTL 自动删除；LastPushedAt 用于按行程限流推送。
public sealed record TripLiveLocation(
    string TripId,
    string DriverId,
    string PropertyId,
    LocationPoint Point,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? LastPushedAt,
    DateTimeOffset ExpiresAt);
