using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Auditing.Ports;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.DriverTrips.Commands;
using LMS.Booking.Application.DriverTrips.Ports;
using LMS.Booking.Application.DriverTrips.Services;
using LMS.Booking.Application.Notifications.Services;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DriverTrips.Handlers;

// 权威指派（Dispatch 接单协作的 Booking 侧提交）：在一个本地事务内检查行程可分配、写入指派与审计。
// 以 OfferId 幂等：同一邀请重试得到同一结果，不会产生第二笔指派，也不会因响应丢失而改派他人。
// 同一司机同时只能有一个执行中的行程，由 trips.activeDriverId 的唯一部分索引原子保证；违反时按状态冲突处理。
internal sealed class AssignDriverHandler(
    ITransactionRunner transactionRunner,
    ITripRepository trips,
    IDriverTripRepository driverTrips,
    IAuditRecordRepository auditRecords,
    TripEventNotifier notifier,
    TripNotificationRecorder events,
    TimeProvider timeProvider) : IAssignDriverHandler
{
    private const string Operation = "trip.assign-driver";
    private const string ResourceType = "trip";

    public async Task<OperationResult<Trip>> HandleAsync(AssignDriverCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await transactionRunner.ExecuteAsync(
                (context, token) => AssignAsync(command, context, token),
                cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.StateConflict);
        }
    }

    private async Task<OperationResult<Trip>> AssignAsync(AssignDriverCommand command, ITransactionContext context, CancellationToken cancellationToken)
    {
        var trip = await driverTrips.FindByIdAsync(command.TripId, context, cancellationToken);
        if (trip is null)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.NotFound);
        }

        if (trip.Assignment is { } existing)
        {
            return existing.OfferId == command.OfferId && existing.DriverId == command.DriverId
                ? OperationResult<Trip>.Succeeded(trip)
                : OperationResult<Trip>.Failed(OperationOutcome.StateConflict);
        }

        if (!trip.CanBeAssigned || trip.PropertyId != command.PropertyId)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.StateConflict);
        }

        return await CommitAsync(trip, command, context, cancellationToken);
    }

    private async Task<OperationResult<Trip>> CommitAsync(
        Trip trip,
        AssignDriverCommand command,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var assigned = trip.AssignDriver(new DriverAssignment(command.OfferId, command.DriverId, command.DriverName, command.VehicleLabel, now));
        if (!await trips.TryReplaceAsync(assigned, trip.Version, context, cancellationToken))
        {
            return OperationResult<Trip>.Failed(OperationOutcome.VersionConflict);
        }

        await auditRecords.InsertAsync(
            AuditRecordFactory.Succeeded(command.Context, Operation, ResourceType, trip.TripId, now, "driver assigned", trip.PropertyId, null),
            context,
            cancellationToken);
        await notifier.NotifyBookerAsync(assigned, TripEventType.DriverAssigned, context, cancellationToken);
        await events.RecordDriverAssignedAsync(assigned, command.OfferId, command.Context.TraceId, context, cancellationToken);

        return OperationResult<Trip>.Succeeded(assigned with { Version = trip.Version + 1 });
    }
}
