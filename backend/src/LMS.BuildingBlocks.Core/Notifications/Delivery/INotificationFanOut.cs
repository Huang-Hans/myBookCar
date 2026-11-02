using LMS.BuildingBlocks.Core.Notifications.Events;

namespace LMS.BuildingBlocks.Core.Notifications.Delivery;

// 把按物业扇出的源事件物化为每位授权收件人的个人事件（以及站内通知）。
// Retry 表示收件人目录暂时不可用或物化未完成；成功物化后源事件由实现置为 Materialized。
public interface INotificationFanOut
{
    Task<NotificationDeliveryResult> MaterializeAsync(NotificationEvent source, string leaseOwner, CancellationToken cancellationToken);
}
