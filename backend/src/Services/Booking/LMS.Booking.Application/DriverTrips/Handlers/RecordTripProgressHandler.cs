using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Auditing.Ports;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.DriverTrips.Commands;
using LMS.Booking.Application.DriverTrips.Ports;
using LMS.Booking.Application.DriverTrips.Services;
using LMS.Booking.Application.Notifications.Services;
using LMS.Booking.Application.TripLocations.Services;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DriverTrips.Handlers;

// 司机履约动作：每次重新校验司机绑定（行程须归属本人）、版本与状态机，目标状态由服务端决定。
// 以幂等键重放：同一键提交同一动作返回当前行程；同一键提交不同动作为幂等键冲突。
// 只有服务端收到并成功提交的动作才是权威事实，不接收客户端自报的完成。
internal sealed class RecordTripProgressHandler(
    ITransactionRunner transactionRunner,
    ITripRepository trips,
    IDriverTripRepository driverTrips,
    IAuditRecordRepository auditRecords,
    TripEventNotifier notifier,
    TripNotificationRecorder events,
    TripDriverReleaseNotifier releases,
    TripLocationEndNotifier locationEnds,
    TimeProvider timeProvider) : IRecordTripProgressHandler
{
    private const string Operation = "trip.driver-progress";
    private const string ResourceType = "trip";

    // 完成动作提交后通知 Dispatch 释放司机，并通知查看者位置共享结束。
    public async Task<OperationResult<Trip>> HandleAsync(RecordTripProgressCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await transactionRunner.ExecuteAsync(
                (context, token) => RecordAsync(command, context, token),
                cancellationToken);

            return await locationEnds.AfterCommitAsync(await releases.AfterCommitAsync(result, cancellationToken), cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.StateConflict);
        }
    }

    private async Task<OperationResult<Trip>> RecordAsync(RecordTripProgressCommand command, ITransactionContext context, CancellationToken cancellationToken)
    {
        var trip = await driverTrips.FindForDriverAsync(command.TripId, command.DriverId, context, cancellationToken);
        if (trip is null)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.NotFound);
        }

        var replay = ReplayOf(trip, command);
        if (replay is not null)
        {
            return replay;
        }

        return FulfillmentRules.NextAction(trip) == command.Action
            ? await CommitAsync(trip, command, context, cancellationToken)
            : OperationResult<Trip>.Failed(OperationOutcome.StateConflict);
    }

    // 只有业务状态实际改变才产生客户事件。
    private Task RecordStatusChangeAsync(
        Trip before,
        Trip after,
        RecordTripProgressCommand command,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        after.Status == before.Status
            ? Task.CompletedTask
            : events.RecordStatusChangedAsync(after, after.Status, command.Context.TraceId, context, cancellationToken);

    private static OperationResult<Trip>? ReplayOf(Trip trip, RecordTripProgressCommand command)
    {
        var previous = trip.Events.FirstOrDefault(item => item.IdempotencyKey == command.IdempotencyKey);
        if (previous is null)
        {
            return null;
        }

        return previous.Type == FulfillmentRules.EventTypeOf(command.Action)
            ? OperationResult<Trip>.Replayed(trip)
            : OperationResult<Trip>.Failed(OperationOutcome.IdempotencyKeyReused);
    }

    private async Task<OperationResult<Trip>> CommitAsync(
        Trip trip,
        RecordTripProgressCommand command,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var updated = trip.ApplyProgress(command.Action, command.DriverId, now, command.IdempotencyKey);
        if (!await trips.TryReplaceAsync(updated, trip.Version, context, cancellationToken))
        {
            return OperationResult<Trip>.Failed(OperationOutcome.VersionConflict);
        }

        await auditRecords.InsertAsync(
            AuditRecordFactory.Succeeded(
                command.Context, Operation, ResourceType, trip.TripId, now, $"progress: {WireEnum.ToWire(command.Action)}", trip.PropertyId, null),
            context,
            cancellationToken);
        await notifier.NotifyBookerAsync(updated, updated.Events[^1].Type, context, cancellationToken);
        await RecordStatusChangeAsync(trip, updated, command, context, cancellationToken);

        return OperationResult<Trip>.Created(updated with { Version = trip.Version + 1 });
    }
}
