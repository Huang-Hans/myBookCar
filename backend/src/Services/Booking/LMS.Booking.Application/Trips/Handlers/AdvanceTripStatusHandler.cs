using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Auditing.Ports;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.ConsoleNotifications.Services;
using LMS.Booking.Application.DriverTrips.Services;
using LMS.Booking.Application.Notifications.Services;
using LMS.Booking.Application.TripLocations.Services;
using LMS.Booking.Application.Trips.Commands;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.Notifications;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;

namespace LMS.Booking.Application.Trips.Handlers;

// 服务间状态推进（/internal，实施基线 D6）：按状态机原子推进，审计与站内通知同事务。
// 一期由运维手工调用，二期由司机 App 复用；不持有期望版本，按"当前状态"条件更新防并发回退。
internal sealed class AdvanceTripStatusHandler(
    ITransactionRunner transactionRunner,
    ITripRepository trips,
    IAuditRecordRepository auditRecords,
    ConsoleNotificationPublisher notifications,
    TripNotificationRecorder events,
    TripDriverReleaseNotifier releases,
    TripLocationEndNotifier locationEnds,
    TimeProvider timeProvider) : IAdvanceTripStatusHandler
{
    private const string Operation = "trip.advance";
    private const string ResourceType = "trip";

    public async Task<OperationResult<Trip>> HandleAsync(AdvanceTripStatusCommand command, CancellationToken cancellationToken)
    {
        var current = await trips.FindVisibleAsync(command.TripId, command.Context.Scope, cancellationToken);
        if (current is null)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.NotFound);
        }

        if (!TripTransitions.CanTransition(current.Status, command.Target))
        {
            return OperationResult<Trip>.Failed(OperationOutcome.StateConflict);
        }

        var moved = await transactionRunner.ExecuteAsync(
            async (context, token) =>
            {
                if (!await trips.TryAdvanceAsync(command.TripId, current.Status, command.Target, token))
                {
                    return false;
                }

                await auditRecords.InsertAsync(
                    AuditRecordFactory.Succeeded(
                        command.Context, Operation, ResourceType, command.TripId, timeProvider.GetUtcNow(),
                        $"status: {WireEnum.ToWire(command.Target)}", current.PropertyId, null),
                    context,
                    token);
                await notifications.PublishAsync(
                    command.Context.Actor,
                    NotificationOf(command.Target),
                    $"Trip {current.TripReference} moved to {WireEnum.ToWire(command.Target)}.",
                    current.TripReference,
                    NotificationTarget.ForTrip(current.TripId),
                    context,
                    token);
                await events.RecordStatusChangedAsync(current, command.Target, command.Context.TraceId, context, token);

                return true;
            },
            cancellationToken);

        if (!moved)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.StateConflict);
        }

        var advanced = await trips.FindByIdAsync(command.TripId, cancellationToken);

        return advanced is null
            ? OperationResult<Trip>.Failed(OperationOutcome.NotFound)
            : await locationEnds.AfterCommitAsync(await releases.AfterCommitAsync(OperationResult<Trip>.Succeeded(advanced), cancellationToken), cancellationToken);
    }

    // 原型通知类型只有已分配/已抵达/完成；途中两态归并为 DRIVER_ARRIVED，其余归 SYSTEM_ALERT。
    private static NotificationType NotificationOf(TripStatus target) =>
        target switch
        {
            TripStatus.DriverAssigned => NotificationType.DriverAssigned,
            TripStatus.DriverEnRoute or TripStatus.ArrivedAtPickup => NotificationType.DriverArrived,
            TripStatus.Completed => NotificationType.TripCompleted,
            _ => NotificationType.SystemAlert
        };
}
