using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Coupons;
using LMS.Booking.Application.DriverTrips.Services;
using LMS.Booking.Application.Notifications.Services;
using LMS.Booking.Application.SelfService.Commands;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Application.SelfService.Services;
using LMS.Booking.Application.TripLocations.Services;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Trips;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.SelfService.Handlers;

// 客户自助取消。能力与政策由服务端状态决定（进行中与终态不可取消，可选取消窗口 SelfService:CancellationCutoffMinutes），
// 原因码取部署配置的自助取消原因并校验其存在于 Platform 清单；取消经 DecisionWorkflow（幂等键 + 版本 + 审计同事务），
// 已指派行程取消后保留指派，司机下次读取时看到该行程进入已取消历史。
internal sealed class CustomerCancellationHandler(
    DecisionWorkflow workflow,
    ITripRepository trips,
    IGuestTripRepository guestTrips,
    IPlatformSelfCatalog catalog,
    SelfCancellationRules rules,
    TripNotificationRecorder events,
    TripDriverReleaseNotifier releases,
    TripLocationEndNotifier locationEnds,
    CouponLedger coupons,
    IOptions<SelfServiceOptions> options,
    TimeProvider timeProvider) : ICustomerCancellationHandler
{
    private const string Operation = "trip.self-cancel";
    private const string ResourceType = "trip";
    private const string DefaultComments = "Cancelled by the customer";

    // 期望版本已取消：流程不再比较，仓储仍以读到的版本做条件写入。
    private const long NoExpectedVersion = 0;

    public async Task<OperationResult<SelfCancellationPolicy>> GetPolicyAsync(string tripId, string guestId, CancellationToken cancellationToken)
    {
        var trip = await guestTrips.FindForGuestAsync(tripId, guestId, cancellationToken);
        if (trip is null)
        {
            return OperationResult<SelfCancellationPolicy>.Failed(OperationOutcome.NotFound);
        }

        var property = await catalog.GetPropertyAsync(trip.PropertyId, cancellationToken);
        if (!property.IsSuccess)
        {
            return OperationResult<SelfCancellationPolicy>.Failed(OperationOutcome.SourceUnavailable);
        }

        var blocked = rules.BlockedReasonOf(trip);

        return OperationResult<SelfCancellationPolicy>.Succeeded(new SelfCancellationPolicy(
            trip, blocked is null, blocked, property.Value!.CancellationPolicyReference, options.Value.CancellationReasonCode));
    }

    public async Task<OperationResult<Trip>> CancelAsync(CancelSelfServiceTripCommand command, CancellationToken cancellationToken)
    {
        var trip = await guestTrips.FindForGuestAsync(command.TripId, command.GuestId, cancellationToken);
        if (trip is null)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.NotFound);
        }

        if (rules.BlockedReasonOf(trip) is not null)
        {
            if (trip.Status == TripStatus.Cancelled)
            {
                var again = await coupons.RestoreAsync(trip.CouponRedemptionKey, cancellationToken);
                if (again is not null || !string.IsNullOrWhiteSpace(trip.CouponRedemptionKey))
                {
                    return again ?? OperationResult<Trip>.Succeeded(trip);
                }
            }

            return OperationResult<Trip>.Failed(OperationOutcome.StateConflict);
        }

        var reason = await catalog.GetCancellationReasonAsync(options.Value.CancellationReasonCode, cancellationToken);
        var property = await catalog.GetPropertyAsync(trip.PropertyId, cancellationToken);
        if (!reason.IsSuccess || !property.IsSuccess)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.SourceUnavailable);
        }

        var result = await workflow.ExecuteAsync(BuildSpecification(command, property.Value!.CancellationPolicyReference), cancellationToken);
        var finished = await locationEnds.AfterCommitAsync(await releases.AfterCommitAsync(result, cancellationToken), cancellationToken);
        if (!finished.IsSuccess)
        {
            return finished;
        }

        var restored = await coupons.RestoreAsync(finished.Value!.CouponRedemptionKey, cancellationToken);
        return restored ?? finished;
    }

    private DecisionSpecification<Trip> BuildSpecification(CancelSelfServiceTripCommand command, string policyReference) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            new PatchSpecification<Trip>(
                command.TripId,
                NoExpectedVersion,
                (id, context, token) => guestTrips.FindForGuestAsync(id, command.GuestId, context, token),
                trip => trip.Version,
                (loaded, _, _) => Task.FromResult(Cancel(loaded, command, policyReference)),
                (updated, expected, context, token) => ReplaceAndRecordAsync(updated, expected, command.Context.TraceId, context, token),
                (_, after, occurredAt) => AuditRecordFactory.Succeeded(
                    command.Context, Operation, ResourceType, after.TripId, occurredAt,
                    $"cancelled: {options.Value.CancellationReasonCode}", after.PropertyId, null)),
            (id, token) => guestTrips.FindForGuestAsync(id, command.GuestId, token));

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

    // 事务内按最新状态再次检查，避免读取之后司机已开始行程等并发改变。
    private OperationResult<Trip> Cancel(Trip loaded, CancelSelfServiceTripCommand command, string policyReference)
    {
        if (rules.BlockedReasonOf(loaded) is not null)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.StateConflict);
        }

        var cancellation = new TripCancellation(
            options.Value.CancellationReasonCode,
            string.IsNullOrWhiteSpace(command.Comments) ? DefaultComments : command.Comments,
            command.GuestId,
            timeProvider.GetUtcNow(),
            policyReference);

        return OperationResult<Trip>.Succeeded(loaded.Cancel(cancellation));
    }
}
