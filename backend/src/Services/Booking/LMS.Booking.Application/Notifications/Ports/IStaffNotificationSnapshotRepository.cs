using LMS.Booking.Application.Notifications.Models;

namespace LMS.Booking.Application.Notifications.Ports;

// 客服专用快照：只含本人当前可见的 BOOKING_CREATED 站内通知，按 occurredAt 降序、notificationId 升序；
// 旧审批、取消等通知没有来源元数据，不进入这个入口。
public interface IStaffNotificationSnapshotRepository
{
    Task<StaffNotificationSnapshot> ReadAsync(StaffNotificationSnapshotQuery query, CancellationToken cancellationToken);
}
