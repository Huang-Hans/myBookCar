namespace LMS.Booking.Domain.Trips;

// 履约轨迹事件：服务端提交时间、操作者与幂等键。Status 是事件发生后的主状态（子阶段事件保持 IN_PROGRESS）。
// IdempotencyKey 用于重复提交同一动作时返回同一结果，仅司机动作带有。
public sealed record TripEvent(
    string EventId,
    TripEventType Type,
    TripStatus Status,
    string Actor,
    DateTimeOffset OccurredAt,
    string? IdempotencyKey)
{
    public static TripEvent Create(TripEventType type, TripStatus status, string actor, DateTimeOffset occurredAt, string? idempotencyKey) =>
        new("evt_" + Guid.CreateVersion7().ToString("N"), type, status, actor, occurredAt, idempotencyKey);
}
