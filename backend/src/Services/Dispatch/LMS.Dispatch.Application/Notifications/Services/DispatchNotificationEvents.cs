using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Notifications.Services;

// Dispatch 登记的三类司机事件的构造。事件标识由邀请稳定推导，重试得到同一个值；
// 摘要为纯文本短语，不含接送地址、客户信息等明细（司机重新查询现有邀请接口取得）。
internal static class DispatchNotificationEvents
{
    // 新邀请：有效期内才提示，ValidUntil 即邀请过期时间。
    public static NotificationEventDraft OfferCreated(DriverOffer offer, string traceId) =>
        For(offer, RealtimeEventType.DriverOfferCreated, $"You have a new transfer order {offer.TripReference}.", traceId, offer.CreatedAt, offer.ExpiresAt);

    // 邀请已关闭（拒绝、过期、被取代），司机清除对应提示；最终状态以现有查询为准。
    public static NotificationEventDraft OfferClosed(DriverOffer closed, string traceId, DateTimeOffset now) =>
        For(closed, RealtimeEventType.DriverOfferClosed, $"Transfer order {closed.TripReference} is no longer open.", traceId, closed.RespondedAt ?? now, null);

    // 接受已最终提交（邀请 ACCEPTED 且司机 ON_TRIP）；ACCEPTING 处理中不会走到这里。
    public static NotificationEventDraft OfferAccepted(DriverOffer accepted, string traceId, DateTimeOffset now) =>
        For(accepted, RealtimeEventType.DriverOfferAccepted, $"Transfer order {accepted.TripReference} was accepted.", traceId, now, null);

    private static NotificationEventDraft For(
        DriverOffer offer,
        RealtimeEventType type,
        string summary,
        string traceId,
        DateTimeOffset occurredAt,
        DateTimeOffset? validUntil) =>
        new(
            NotificationEventIds.For(type, offer.OfferId),
            type,
            RealtimeRecipientKind.Driver,
            offer.DriverId,
            offer.PropertyId,
            summary,
            offer.TripReference,
            RealtimeTargetTypes.DriverOffer,
            offer.OfferId,
            traceId,
            occurredAt,
            validUntil);
}
