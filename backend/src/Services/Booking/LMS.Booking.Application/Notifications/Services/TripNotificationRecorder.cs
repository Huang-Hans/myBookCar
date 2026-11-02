using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Notifications.Ports;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Notifications.Services;

// 订单相关用例在自身事务内登记实时通知事件的入口。只在业务事实真正首次提交的分支调用：
// 幂等重放、同一决定重试、只改备注和条件写入失败都不调用，因此不会重复或虚构事件。
internal sealed class TripNotificationRecorder(INotificationEventRecorder recorder, TimeProvider timeProvider)
{
    public Task RecordBookingCreatedAsync(Trip trip, string traceId, ITransactionContext context, CancellationToken cancellationToken) =>
        recorder.RecordAsync(BookingNotificationEvents.BookingCreated(trip, traceId), context, cancellationToken);

    public Task RecordStaffBookingCreatedAsync(Trip trip, string traceId, ITransactionContext context, CancellationToken cancellationToken) =>
        recorder.RecordAsync(BookingNotificationEvents.BookingCreated(trip, traceId) with
        {
            Summary = $"New staff booking {trip.TripReference}."
        }, context, cancellationToken);

    public Task RecordDriverAssignedAsync(Trip assigned, string offerId, string traceId, ITransactionContext context, CancellationToken cancellationToken) =>
        recorder.RecordAsync(BookingNotificationEvents.DriverAssigned(assigned, offerId, traceId, timeProvider.GetUtcNow()), context, cancellationToken);

    public Task RecordStatusChangedAsync(Trip trip, TripStatus status, string traceId, ITransactionContext context, CancellationToken cancellationToken) =>
        recorder.RecordAsync(BookingNotificationEvents.TripStatusChanged(trip, status, traceId, timeProvider.GetUtcNow()), context, cancellationToken);
}
