using LMS.BuildingBlocks.Core.Notifications.Events;

namespace LMS.BuildingBlocks.Core.Notifications.Delivery;

// 把一条已登记的个人事件发布给实时通道。实现位于承载 Hub 的 Api 层；发布成功只代表交给了 Hub。
public interface INotificationDeliverer
{
    Task<NotificationDeliveryResult> DeliverAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken);
}
