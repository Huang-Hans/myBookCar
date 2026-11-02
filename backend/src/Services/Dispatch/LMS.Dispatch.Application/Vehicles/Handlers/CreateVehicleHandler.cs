using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Application.Idempotency.Ports;
using LMS.Dispatch.Application.Idempotency.Services;
using LMS.Dispatch.Application.Vehicles.Commands;
using LMS.Dispatch.Application.Vehicles.Models;
using LMS.Dispatch.Application.Vehicles.Ports;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Vehicles;

namespace LMS.Dispatch.Application.Vehicles.Handlers;

// 新增车辆主档。车牌全库唯一；指派司机时该司机必须启用，且尚未指派到其他车辆。
internal sealed class CreateVehicleHandler(
    CreateWorkflow workflow,
    IVehicleRepository vehicles,
    IDriverRepository drivers,
    IPlatformDirectory directory,
    IIdempotencyRecordRepository idempotencyRecords,
    TimeProvider timeProvider) : ICreateVehicleHandler
{
    private const string Operation = "vehicle.create";
    private const string ResourceType = "vehicle";

    public async Task<OperationResult<VehicleCard>> HandleAsync(CreateVehicleCommand command, CancellationToken cancellationToken)
    {
        Vehicle? candidate = null;
        if (!await IsReplayAsync(command, cancellationToken))
        {
            var prepared = await PrepareAsync(command, cancellationToken);
            if (prepared.Failure is not null)
            {
                return prepared.Failure;
            }

            candidate = prepared.Vehicle;
        }

        var created = await workflow.ExecuteAsync(BuildSpecification(command, candidate), cancellationToken);
        if (!created.IsSuccess)
        {
            return OperationResult<VehicleCard>.FailureOf(created);
        }

        var vehicle = created.Value!;
        string? driverName = null;
        if (vehicle.DriverId is not null)
        {
            driverName = (await drivers.FindByIdAsync(vehicle.DriverId, cancellationToken))?.Name;
        }

        return created.Outcome == OperationOutcome.Replayed
            ? OperationResult<VehicleCard>.Replayed(new VehicleCard(vehicle, driverName))
            : OperationResult<VehicleCard>.Created(new VehicleCard(vehicle, driverName));
    }

    private async Task<bool> IsReplayAsync(CreateVehicleCommand command, CancellationToken cancellationToken)
    {
        var fingerprint = IdempotencyFingerprintFactory.Create(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm());

        return await idempotencyRecords.FindActiveAsync(fingerprint, timeProvider.GetUtcNow(), cancellationToken) is not null;
    }

    private async Task<(OperationResult<VehicleCard>? Failure, Vehicle? Vehicle)> PrepareAsync(
        CreateVehicleCommand command,
        CancellationToken cancellationToken)
    {
        var property = await DriverPropertyCheck.CheckAsync<VehicleCard>(directory, command.Context.Scope, command.PropertyId, cancellationToken);
        if (property is not null)
        {
            return (property, null);
        }

        if (command.DriverId is not null)
        {
            var driver = await drivers.FindVisibleAsync(command.DriverId, command.Context.Scope, cancellationToken);
            if (driver is null)
            {
                return (OperationResult<VehicleCard>.Invalid([new FieldIssue("driverId", "Unknown driver")]), null);
            }

            if (driver.PropertyId != command.PropertyId)
            {
                return (OperationResult<VehicleCard>.Invalid([new FieldIssue("driverId", "Select a driver from the vehicle property.")]), null);
            }

            if (driver.Status != DriverStatus.Active)
            {
                return (OperationResult<VehicleCard>.Invalid([new FieldIssue("driverId", "Must be an active driver")]), null);
            }

            if (await vehicles.IsDriverAssignedAsync(command.DriverId, cancellationToken))
            {
                return (OperationResult<VehicleCard>.Invalid([new FieldIssue("driverId", "Already assigned to another vehicle")]), null);
            }
        }

        if (await vehicles.FindByPlateAsync(command.Plate, cancellationToken) is not null)
        {
            return (OperationResult<VehicleCard>.Invalid([new FieldIssue("plate", "Already exists")]), null);
        }

        var candidate = Vehicle.Create(
            command.Plate, command.Model, command.Grade, command.PropertyId, command.Seats, command.MileageKm, command.Status, command.DriverId);
        var issues = VehicleRules.Validate(candidate);
        if (issues.Count > 0)
        {
            return (OperationResult<VehicleCard>.Invalid(issues), null);
        }

        return (null, candidate);
    }

    private CreateSpecification<Vehicle> BuildSpecification(CreateVehicleCommand command, Vehicle? candidate) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(candidate!),
            vehicle => vehicle.VehicleId,
            (vehicle, context, token) => vehicles.InsertAsync(vehicle, context, token),
            (id, token) => vehicles.FindByIdAsync(id, token),
            (vehicle, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, vehicle.VehicleId, now, vehicle.PropertyId));
}
