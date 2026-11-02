using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DispatchOrders.Models;

public static class DispatchOrderGroups
{
    private static readonly TripStatus[] AssignedStatuses =
        [TripStatus.DriverAssigned, TripStatus.DriverEnRoute, TripStatus.ArrivedAtPickup, TripStatus.InProgress];

    // 分组对应的订单状态；Any 返回 null 表示不按状态限制。
    public static IReadOnlyList<TripStatus>? StatusesOf(DispatchOrderGroup group) => group switch
    {
        DispatchOrderGroup.PendingConfirmation => [TripStatus.Pending],
        DispatchOrderGroup.Confirmed => [TripStatus.Confirmed],
        DispatchOrderGroup.Unassigned => [TripStatus.Pending, TripStatus.Confirmed],
        DispatchOrderGroup.Assigned => AssignedStatuses,
        DispatchOrderGroup.Completed => [TripStatus.Completed],
        DispatchOrderGroup.Cancelled => [TripStatus.Cancelled],
        _ => null
    };
}
