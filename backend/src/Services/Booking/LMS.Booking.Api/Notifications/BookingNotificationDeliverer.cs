using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Core.Notifications.Delivery;
using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.BuildingBlocks.Realtime.Hubs;
using LMS.Booking.Application.Notifications.Services;
using Microsoft.AspNetCore.SignalR;

namespace LMS.Booking.Api.Notifications;

// 把 Booking 的个人事件发布到客服 / 客户 Hub 的个人组。发布成功只表示交给了 Hub（空组也算），
// 不表示在线、已展示、已读或已处理。客服事件发布前先复核该收件人当前仍被授权。
internal sealed class BookingNotificationDeliverer(
    IHubContext<StaffNotificationHub> staffHub,
    IHubContext<CustomerNotificationHub> customerHub,
    StaffDeliveryGate staffGate) : INotificationDeliverer
{
    private const string UnsupportedRecipient = "UNSUPPORTED_RECIPIENT";

    public async Task<NotificationDeliveryResult> DeliverAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken)
    {
        switch (notificationEvent.Draft.RecipientKind)
        {
            case RealtimeRecipientKind.Staff:
                return await DeliverToStaffAsync(notificationEvent, cancellationToken);
            case RealtimeRecipientKind.Customer:
                await SendAsync(customerHub.Clients, notificationEvent, cancellationToken);
                return NotificationDeliveryResult.Delivered;
            default:
                return NotificationDeliveryResult.Skipped(UnsupportedRecipient);
        }
    }

    private async Task<NotificationDeliveryResult> DeliverToStaffAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken)
    {
        var rejection = await staffGate.CheckAsync(notificationEvent, cancellationToken);
        if (rejection is not null)
        {
            return rejection;
        }

        await SendAsync(staffHub.Clients, notificationEvent, cancellationToken);

        return NotificationDeliveryResult.Delivered;
    }

    private static Task SendAsync(IHubClients clients, NotificationEvent notificationEvent, CancellationToken cancellationToken)
    {
        var group = NotificationHubGroups.For(notificationEvent.Draft.RecipientKind, notificationEvent.Draft.RecipientId!);

        return clients.Group(group).SendAsync(NotificationHubRoutes.ClientMethod, NotificationMessageFactory.Create(notificationEvent), cancellationToken);
    }
}
