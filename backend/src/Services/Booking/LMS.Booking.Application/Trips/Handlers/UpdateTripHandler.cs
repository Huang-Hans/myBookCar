using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Trips.Commands;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Application.Trips.Services;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Application.Trips.Handlers;

// 修改行程（HA-10）：仅 PENDING~DRIVER_ASSIGNED 可改（TripTransitions.CanModify）。
// 合并后的输入在事务外重新走校验链（地点解析、资格、容量、时间窗、配额）；事务内只做纯合并与条件替换。
internal sealed class UpdateTripHandler(
    PatchWorkflow workflow,
    TripEligibilityService eligibility,
    ITripRepository trips,
    TimeProvider timeProvider) : IUpdateTripHandler
{
    private const string Operation = "trip.update";
    private const string ResourceType = "trip";

    public async Task<OperationResult<Trip>> HandleAsync(UpdateTripCommand command, CancellationToken cancellationToken)
    {
        if (command.ChangedFields().Count == 0)
        {
            return OperationResult<Trip>.Invalid([new FieldIssue(string.Empty, "At least one field is required")]);
        }

        var current = await trips.FindVisibleAsync(command.TripId, command.Context.Scope, cancellationToken);
        if (current is null)
        {
            return OperationResult<Trip>.Failed(OperationOutcome.NotFound);
        }

        if (!TripTransitions.CanModify(current.Status))
        {
            return OperationResult<Trip>.Failed(OperationOutcome.StateConflict);
        }

        var pickupLocationId = command.PickupLocationId.ValueOr(current.Route.PickupLocationId);
        var dropoffLocationId = command.DropoffLocationId.ValueOr(current.Route.DropoffLocationId);
        var pickupAt = command.PickupAt.ValueOr(current.PickupAt);
        var pickupMode = command.PickupMode.ValueOr(current.PickupMode);
        var serviceType = command.ServiceType.ValueOr(current.ServiceType);
        var vehicleClass = command.VehicleClass.ValueOr(current.VehicleClass);
        var passengers = command.Passengers.ValueOr(current.Passengers);
        var luggage = command.Luggage.ValueOr(current.Luggage);
        var specialRequests = command.SpecialRequests.ValueOrCurrent(current.SpecialRequests);
        var confirmationLanguage = command.ConfirmationLanguage.ValueOr(current.ConfirmationLanguage);

        var verdict = await eligibility.EvaluateAsync(
            new TripEvaluationRequest(
                current.Guest.GuestId, current.PropertyId, pickupLocationId, dropoffLocationId,
                pickupAt, pickupMode, vehicleClass, passengers, luggage),
            cancellationToken);

        if (!verdict.Result.IsSuccess)
        {
            return verdict.Result.ToTripResult();
        }

        var passed = verdict.Result.Value!;

        return await workflow.ExecuteAsync(new PatchSpecification<Trip>(
            command.TripId,
            command.ExpectedVersion,
            (id, context, token) => trips.FindVisibleAsync(id, command.Context.Scope, context, token),
            trip => trip.Version,
            (loaded, _, _) => Task.FromResult(OperationResult<Trip>.Succeeded(loaded.Update(
                passed.Route,
                pickupAt,
                pickupMode,
                serviceType,
                vehicleClass,
                passengers,
                luggage,
                specialRequests,
                confirmationLanguage,
                passed.Capacity.PointsRequired).WithModification(ModificationOf(command)))),
            (updated, expected, context, token) => trips.TryReplaceAsync(updated, expected, context, token),
            (_, after, occurredAt) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, after.TripId, occurredAt, command.ChangedFields(), after.PropertyId)),
            cancellationToken);
    }

    // 修改记录与行程更新在同一次提交里持久化；操作者与时间来自服务端。
    private TripModification ModificationOf(UpdateTripCommand command) => new(
        command.ModificationReason,
        command.ModificationComments,
        command.Context.Actor,
        command.Context.ActorName,
        timeProvider.GetUtcNow(),
        command.ChangedFields());
}
