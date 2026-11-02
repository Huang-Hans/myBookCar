using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Coupons;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.ConsoleNotifications.Services;
using LMS.Booking.Application.Notifications.Services;
using LMS.Booking.Application.Staff.Ports;
using LMS.Booking.Application.Trips.Commands;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Application.Trips.Services;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Notifications;
using LMS.Booking.Domain.Staff;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Application.Trips.Handlers;

// 代订建单（HA-01/HA-03）：校验链前置，创建经 CreateWorkflow（幂等 201/Replayed），
// 行程、ELIGIBLE 资格档案、站内通知与最近地点在同一个本地事务内提交。
internal sealed class CreateTripHandler(
    CreateWorkflow workflow,
    TripEligibilityService eligibility,
    QualificationRecorder qualificationRecorder,
    ConsoleNotificationPublisher notifications,
    IStaffPreferencesRepository staffPreferences,
    ITripRepository trips,
    CouponLedger coupons,
    TimeProvider timeProvider,
    ConsoleBookingProductRules products,
    TripNotificationRecorder events) : ICreateTripHandler
{
    private const string Operation = "trip.create";
    private const string ResourceType = "trip";

    // ASAP 单的固定接车缓冲（对齐原型 "Within 15 minutes"）。
    internal static readonly TimeSpan AsapBuffer = TimeSpan.FromMinutes(15);

    public async Task<OperationResult<Trip>> HandleAsync(CreateTripCommand command, CancellationToken cancellationToken)
    {
        if (!command.Context.Scope.Allows(command.PropertyId))
        {
            return OperationResult<Trip>.Failed(OperationOutcome.Forbidden);
        }

        if (command.Product is { } product)
        {
            var selection = await products.CheckAsync(product, command.PickupLocationId, command.DropoffLocationId, command.PickupMode, cancellationToken);
            if (!selection.IsSuccess) return OperationResult<Trip>.FailureOf(selection);
            command = command with { ServiceType = selection.Value };
        }

        var pickupAt = command.PickupMode == PickupMode.Asap
            ? timeProvider.GetUtcNow().Add(AsapBuffer)
            : command.PickupAt;
        if (pickupAt is null)
        {
            return OperationResult<Trip>.Invalid([new FieldIssue("pickupAt", "Required unless pickupMode is ASAP")]);
        }

        var verdict = await eligibility.EvaluateAsync(
            new TripEvaluationRequest(
                command.GuestId, command.PropertyId, command.PickupLocationId, command.DropoffLocationId,
                pickupAt.Value, command.PickupMode, command.VehicleClass, command.Passengers, command.Luggage),
            cancellationToken);

        if (!verdict.Result.IsSuccess)
        {
            // 资格不满足：先旁路登记待经理复核档案（D1），422 仍是主响应。
            if (verdict.RequiresManagerReview && verdict.Guest is not null)
            {
                await qualificationRecorder.RecordRequiredAsync(
                    verdict.Guest,
                    command.PropertyId,
                    "Console transfer request did not pass the qualification check",
                    command.Context,
                    null,
                    cancellationToken);
            }

            return verdict.Result.ToTripResult();
        }

        var passed = verdict.Result.Value!;
        var guest = verdict.Guest!;
        string? redemptionKey = null;
        if (!string.IsNullOrWhiteSpace(command.CouponId))
        {
            redemptionKey = command.IdempotencyKey;
            var rejected = await coupons.RedeemAsync(
                new CouponUse(redemptionKey, command.CouponId.Trim(), guest.MemberId, guest.GuestId, command.PropertyId, WireEnum.ToWire(command.VehicleClass)),
                cancellationToken);
            if (rejected is not null)
            {
                return rejected;
            }
        }

        var created = await workflow.ExecuteAsync(new CreateSpecification<Trip>(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            async (context, token) => Trip.Create(
                await trips.NextReferenceAsync(MacauClock.DateOf(timeProvider.GetUtcNow()), context, token),
                command.PropertyId,
                new TripGuest(guest.GuestId, guest.Name, guest.TierCode, guest.RoomNumber, guest.MemberId, guest.MaskedPhone),
                passed.Route,
                pickupAt.Value,
                command.PickupMode,
                command.ServiceType,
                command.VehicleClass,
                command.Passengers,
                command.Luggage,
                command.SpecialRequests,
                passed.Capacity.PointsRequired,
                new BookedBy(command.Context.Actor, command.Context.ActorRole ?? string.Empty, command.Context.ActorName),
                command.ConfirmationLanguage,
                timeProvider.GetUtcNow(),
                product: command.Product,
                couponId: string.IsNullOrWhiteSpace(command.CouponId) ? null : command.CouponId.Trim(),
                couponRedemptionKey: redemptionKey),
            trip => trip.TripId,
            async (trip, context, token) =>
            {
                await trips.InsertAsync(trip, context, token);
                await events.RecordStaffBookingCreatedAsync(trip, command.Context.TraceId, context, token);
                await qualificationRecorder.RecordEligibleAsync(guest, command.PropertyId, command.Context, trip.TripReference, context, token);
                await notifications.PublishAsync(
                    command.Context.Actor,
                    NotificationType.BookingConfirmed,
                    $"Transfer {trip.TripReference} confirmed for {guest.Name}.",
                    trip.TripReference,
                    NotificationTarget.ForTrip(trip.TripId),
                    context,
                    token);
                await RecordRecentLocationsAsync(command.Context.Actor, trip, context, token);
            },
            (id, token) => trips.FindByIdAsync(id, token),
            (trip, occurredAt) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, trip.TripId, occurredAt, trip.PropertyId)),
            cancellationToken);
        if (!created.IsSuccess && redemptionKey is not null)
        {
            await coupons.RestoreAsync(redemptionKey, cancellationToken);
        }

        return created;
    }

    // 建单成功后把取/送地点记入经办人最近使用（去头插，上限 MaxRecent）。
    private async Task RecordRecentLocationsAsync(string userId, Trip trip, ITransactionContext context, CancellationToken cancellationToken)
    {
        var preferences = await staffPreferences.FindAsync(userId, cancellationToken) ?? StaffPreferences.Defaults(userId);
        var recents = new[] { trip.Route.PickupLocationId, trip.Route.DropoffLocationId }
            .Concat(preferences.RecentLocationIds)
            .Distinct(StringComparer.Ordinal)
            .Take(StaffPreferences.MaxRecent)
            .ToList();

        await staffPreferences.UpsertAsync(preferences with { RecentLocationIds = recents }, context, cancellationToken);
    }
}
