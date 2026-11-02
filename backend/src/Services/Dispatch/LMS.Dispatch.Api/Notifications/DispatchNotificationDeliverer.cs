using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Core.Notifications.Delivery;
using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.BuildingBlocks.Realtime.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LMS.Dispatch.Api.Notifications;

// 把 Dispatch 的个人事件发布到司机 Hub 的个人组。发布成功只表示交给了 Hub（空组也算），
// 不表示在线、已展示或已接单。
internal sealed class DispatchNotificationDeliverer(IHubContext<DriverNotificationHub> driverHub) : INotificationDeliverer
{
    private const string UnsupportedRecipient = "UNSUPPORTED_RECIPIENT";

    public async Task<NotificationDeliveryResult> DeliverAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken)
    {
        var draft = notificationEvent.Draft;
        if (draft.RecipientKind != RealtimeRecipientKind.Driver)
        {
            return NotificationDeliveryResult.Skipped(UnsupportedRecipient);
        }

        var group = NotificationHubGroups.For(draft.RecipientKind, draft.RecipientId!);
        await driverHub.Clients.Group(group).SendAsync(NotificationHubRoutes.ClientMethod, NotificationMessageFactory.Create(notificationEvent), cancellationToken);

        return NotificationDeliveryResult.Delivered;
    }
}
