using LMS.BuildingBlocks.Core.Wire;

namespace LMS.BuildingBlocks.Core.Notifications.Events;

// 业务用例在自身事务内登记的一条待发送事件。EventId 由业务事实稳定推导（见 NotificationEventIds），
// 同时是存储主键，因此同一事实只会登记一次。RecipientId 为 null 表示按 PropertyId 扇出给物业范围内的授权员工。
public sealed record NotificationEventDraft(
    string EventId,
    RealtimeEventType EventType,
    RealtimeRecipientKind RecipientKind,
    string? RecipientId,
    string? PropertyId,
    string Summary,
    string? TripReference,
    string TargetType,
    string TargetId,
    string TraceId,
    DateTimeOffset OccurredAt,
    DateTimeOffset? ValidUntil = null)
{
    public bool IsPropertyFanOut => RecipientId is null;

    public string EventTypeWire => WireEnum.ToWire(EventType);
}
