using LMS.Booking.Application.Notifications.Models;

namespace LMS.Booking.Application.Notifications.Handlers;

public interface IStaffNotificationSnapshotHandler
{
    Task<StaffNotificationSnapshot> GetAsync(StaffNotificationSnapshotQuery query, CancellationToken cancellationToken);
}
