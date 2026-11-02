using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Notifications.Services;

// Booking 登记的三类事件的构造。事件标识由业务事实稳定推导，重试得到同一个值；
// 摘要为纯文本短语，不含订单明细、司机档案或个人信息。
internal static class BookingNotificationEvents
{
    private const string NoticePrefix = "notice_";

    // 客户自助下单成功受理（不等于已确认）：按订单物业扇出给授权客服。
    public static NotificationEventDraft BookingCreated(Trip trip, string traceId) =>
        new(
            NotificationEventIds.For(RealtimeEventType.BookingCreated, trip.TripId),
            RealtimeEventType.BookingCreated,
            RealtimeRecipientKind.Staff,
            null,
            trip.PropertyId,
            $"New customer booking {trip.TripReference}.",
            trip.TripReference,
            RealtimeTargetTypes.Trip,
            trip.TripId,
            traceId,
            trip.CreatedAt);

    // Booking 权威指派成功：发给订单受益客户（trip.Guest.GuestId），不是客服经办人。
    public static NotificationEventDraft DriverAssigned(Trip assigned, string offerId, string traceId, DateTimeOffset now) =>
        ForCustomer(
            assigned,
            RealtimeEventType.DriverAssigned,
            NotificationEventIds.For(RealtimeEventType.DriverAssigned, assigned.TripId, offerId),
            $"A driver has been assigned to transfer {assigned.TripReference}.",
            traceId,
            now);

    // 订单状态实际改变（履约推进、取消、状态推进）：发给订单受益客户。
    public static NotificationEventDraft TripStatusChanged(Trip trip, TripStatus status, string traceId, DateTimeOffset now) =>
        ForCustomer(
            trip,
            RealtimeEventType.TripStatusChanged,
            NotificationEventIds.For(RealtimeEventType.TripStatusChanged, trip.TripId, WireEnum.ToWire(status).ToLowerInvariant()),
            $"Transfer {trip.TripReference} is now {WireEnum.ToWire(status)}.",
            traceId,
            now);

    // 扇出源事件对一位收件人的个人事件。
    public static NotificationEventDraft PersonalOf(NotificationEventDraft source, string userId) =>
        source with
        {
            EventId = $"{source.EventId}_{userId}",
            RecipientId = userId
        };

    // 站内通知标识由个人事件稳定推导，物化重试不会产生第二条未读通知。
    public static string NotificationIdOf(NotificationEventDraft personal) => $"{NoticePrefix}{personal.TargetId}_{personal.RecipientId}";

    private static NotificationEventDraft ForCustomer(
        Trip trip,
        RealtimeEventType type,
        string eventId,
        string summary,
        string traceId,
        DateTimeOffset now) =>
        new(
            eventId,
            type,
            RealtimeRecipientKind.Customer,
            trip.Guest.GuestId,
            trip.PropertyId,
            summary,
            trip.TripReference,
            RealtimeTargetTypes.Trip,
            trip.TripId,
            traceId,
            now);
}
