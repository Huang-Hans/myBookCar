using LMS.Booking.Application.Notifications.Models;
using LMS.Booking.Application.Notifications.Ports;

namespace LMS.Booking.Application.Notifications.Handlers;

internal sealed class StaffNotificationSnapshotHandler(IStaffNotificationSnapshotRepository snapshots) : IStaffNotificationSnapshotHandler
{
    public Task<StaffNotificationSnapshot> GetAsync(StaffNotificationSnapshotQuery query, CancellationToken cancellationToken) =>
        snapshots.ReadAsync(query, cancellationToken);
}
