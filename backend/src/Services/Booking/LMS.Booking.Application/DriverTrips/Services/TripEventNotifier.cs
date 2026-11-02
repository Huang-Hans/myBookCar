using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.ConsoleNotifications.Services;
using LMS.Booking.Domain.Notifications;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Wire;

namespace LMS.Booking.Application.DriverTrips.Services;

// 司机履约事件对客服代订经办人的站内通知（与历史上运维推进状态时的通知口径一致）。
// 客户自助订单的经办人是客户本人，不是控制台用户，因此不发站内通知（客户侧通知属 P3）。
internal sealed class TripEventNotifier(ConsoleNotificationPublisher publisher)
{
    public async Task NotifyBookerAsync(Trip trip, TripEventType type, ITransactionContext context, CancellationToken cancellationToken)
    {
        if (trip.Channel != TripChannel.Console || NotificationOf(type) is not { } notification)
        {
            return;
        }

        await publisher.PublishAsync(
            trip.BookedBy.UserId,
            notification,
            $"Trip {trip.TripReference} moved to {WireEnum.ToWire(trip.Status)}.",
            trip.TripReference,
            NotificationTarget.ForTrip(trip.TripId),
            context,
            cancellationToken);
    }

    private static NotificationType? NotificationOf(TripEventType type) => type switch
    {
        TripEventType.DriverAssigned => NotificationType.DriverAssigned,
        TripEventType.ArrivedAtPickup => NotificationType.DriverArrived,
        TripEventType.Completed => NotificationType.TripCompleted,
        _ => null
    };
}
