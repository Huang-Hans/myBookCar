using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Coupons;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.ConsoleNotifications.Services;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.DriverTrips.Services;
using LMS.Booking.Application.Notifications.Services;
using LMS.Booking.Application.TripLocations.Services;
using LMS.Booking.Application.Trips.Commands;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Notifications;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Application.Trips.Handlers;

// 取消行程（HA-10）：原因码必须来自 Platform 上传清单（CF03）；物业政策引用随取消记录落库；
// 取消经 DecisionWorkflow（幂等键 + 期望版本 + 审计同事务），站内通知在成功后补发（幂等重放不重发）。
internal sealed class CancelTripHandler(
    DecisionWorkflow workflow,
    ITripRepository trips,
    IPlatformCatalog catalog,
    ConsoleNotificationPublisher notifications,
    TripNotificationRecorder events,
    TripDriverReleaseNotifier releases,
    TripLocationEndNotifier locationEnds,
    CouponLedger coupons,
    TimeProvider timeProvider) : ICancelTripHandler
{
    private const string Operation = "trip.cancel";
    private const string ResourceType = "trip";

    public async Task<OperationResult<Trip>> HandleAsync(CancelTripCommand command, CancellationToken cancellationToken)
    {
        var current = await trips.FindVisibleAsync(command.TripId, command.Context.Scope, cancellationToken);
        if (current is null)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.NotFound);
        }

        if (!TripTransitions.CanCancel(current.Status))
        {
            if (current.Status == TripStatus.Cancelled)
            {
                var again = await coupons.RestoreAsync(current.CouponRedemptionKey, cancellationToken);
                if (again is not null || !string.IsNullOrWhiteSpace(current.CouponRedemptionKey))
                {
                    return again ?? OperationResult<Trip>.Succeeded(current);
                }
            }

            return OperationResult<Trip>.Failed(OperationOutcome.StateConflict);
        }

        var reason = await catalog.GetCancellationReasonAsync(command.ReasonCode, cancellationToken);
        if (!reason.IsSuccess)
        {
            return reason.Outcome switch
            {
                DirectoryOutcome.NotFound => OperationResult<Trip>.Invalid([new FieldIssue("reasonCode", "Unknown cancellation reason")]),
                DirectoryOutcome.Forbidden => OperationResult<Trip>.Failed(OperationOutcome.Forbidden),
                _ => OperationResult<Trip>.Failed(OperationOutcome.SourceUnavailable)
            };
        }

        var property = await catalog.GetPropertyAsync(current.PropertyId, cancellationToken);
        if (!property.IsSuccess)
        {
            return property.Outcome == DirectoryOutcome.Forbidden
                ? OperationResult<Trip>.Failed(OperationOutcome.Forbidden)
                : OperationResult<Trip>.Failed(OperationOutcome.SourceUnavailable);
        }

        var result = await workflow.ExecuteAsync(new DecisionSpecification<Trip>(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            new PatchSpecification<Trip>(
                command.TripId,
                command.ExpectedVersion,
                (id, context, token) => trips.FindVisibleAsync(id, command.Context.Scope, context, token),
                trip => trip.Version,
                (loaded, _, _) => Task.FromResult(OperationResult<Trip>.Succeeded(loaded.Cancel(new TripCancellation(
                    command.ReasonCode,
                    command.Comments,
                    command.Context.Actor,
                    timeProvider.GetUtcNow(),
                    property.Value!.CancellationPolicyReference)))),
                (updated, expected, context, token) => ReplaceAndRecordAsync(updated, expected, command.Context.TraceId, context, token),
                (_, after, occurredAt) => AuditRecordFactory.Succeeded(
                    command.Context, Operation, ResourceType, after.TripId, occurredAt,
                    $"cancelled: {command.ReasonCode}", after.PropertyId, null)),
            (id, token) => trips.FindByIdAsync(id, token)),
            cancellationToken);

        if (result.IsSuccess && result.Outcome != OperationOutcome.Replayed)
        {
            await notifications.PublishStandaloneAsync(
                command.Context.Actor,
                NotificationType.SystemAlert,
                $"Transfer {current.TripReference} was cancelled ({reason.Value!.Name}).",
                current.TripReference,
                NotificationTarget.ForTrip(current.TripId),
                cancellationToken);
        }

        var finished = await locationEnds.AfterCommitAsync(await releases.AfterCommitAsync(result, cancellationToken), cancellationToken);
        if (!finished.IsSuccess)
        {
            return finished;
        }

        var restored = await coupons.RestoreAsync(finished.Value!.CouponRedemptionKey, cancellationToken);
        return restored ?? finished;
    }

    // 取消与“订单状态变化”实时通知事件在同一个事务内提交。
    private async Task<bool> ReplaceAndRecordAsync(Trip cancelled, long expectedVersion, string traceId, ITransactionContext context, CancellationToken cancellationToken)
    {
        if (!await trips.TryReplaceAsync(cancelled, expectedVersion, context, cancellationToken))
        {
            return false;
        }

        await events.RecordStatusChangedAsync(cancelled, cancelled.Status, traceId, context, cancellationToken);

        return true;
    }
}
