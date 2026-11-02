using LMS.BuildingBlocks.Core.Notifications.Contracts;
using LMS.BuildingBlocks.Core.Notifications.Events;

namespace LMS.BuildingBlocks.Realtime.Hubs;

// 把已登记的个人事件转换成线上消息 NotificationMessageV1。
// notificationId / type 只有客服的站内通知有值（type 固定 SYSTEM_ALERT，不用 BOOKING_CONFIRMED 冒充新订单受理）。
public static class NotificationMessageFactory
{
    public const string StaffNotificationType = "SYSTEM_ALERT";

    public static NotificationMessageV1 Create(NotificationEvent notificationEvent)
    {
        var draft = notificationEvent.Draft;
        var content = new NotificationContentV1(
            notificationEvent.NotificationId,
            notificationEvent.NotificationId is null ? null : StaffNotificationType,
            draft.Summary,
            draft.TripReference,
            draft.OccurredAt.UtcDateTime,
            draft.TargetType,
            draft.TargetId);

        return new NotificationMessageV1(
            NotificationMessageV1.SuccessCode,
            NotificationMessageV1.SuccessMessage,
            new NotificationEventDataV1(draft.EventId, NotificationMessageV1.CurrentSchemaVersion, draft.EventTypeWire, content),
            draft.TraceId);
    }

    public static NotificationContentV1 ContentOf(string notificationId, string summary, string? tripReference, DateTimeOffset occurredAt, string? targetType, string? targetId) =>
        new(notificationId, StaffNotificationType, summary, tripReference, occurredAt.UtcDateTime, targetType, targetId);
}
