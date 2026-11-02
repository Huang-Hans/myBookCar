using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Patching;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Drivers.Commands;
using LMS.Dispatch.Application.Drivers.Handlers;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Application.Shifts.Services;
using LMS.Dispatch.Application.Vehicles.Ports;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Handlers;

// Legacy shift vehicle routes update the driver's binding. They never modify shift documents.
internal sealed class AssignShiftVehicleHandler(
    IDriverShiftRepository shifts, IDriverRepository drivers, IUpdateDriverHandler updateDrivers,
    ShiftCardComposer cards, IVehicleRepository vehicles) : IAssignShiftVehicleHandler
{
    public async Task<OperationResult<ShiftCard>> AssignAsync(AssignShiftVehicleCommand command, CancellationToken cancellationToken)
    {
        var shift = await shifts.FindVisibleAsync(command.ShiftId, command.Context.Scope, cancellationToken);
        if (shift is null) return OperationResult<ShiftCard>.Failed(OperationOutcome.NotFound);
        var vehicle = await vehicles.FindByPlateAsync(command.VehiclePlate, cancellationToken);
        if (vehicle is null || vehicle.PropertyId != shift.PropertyId)
            return OperationResult<ShiftCard>.Invalid([new("vehiclePlate", "The vehicle and driver must belong to the same property.")]);
        return await ChangeBindingAsync(shift, vehicle.VehicleId, command.Context, cancellationToken);
    }

    public async Task<OperationResult<ShiftCard>> ClearAsync(ClearShiftVehicleCommand command, CancellationToken cancellationToken)
    {
        var shift = await shifts.FindVisibleAsync(command.ShiftId, command.Context.Scope, cancellationToken);
        return shift is null ? OperationResult<ShiftCard>.Failed(OperationOutcome.NotFound)
            : await ChangeBindingAsync(shift, null, command.Context, cancellationToken);
    }

    private async Task<OperationResult<ShiftCard>> ChangeBindingAsync(DriverShift shift, string? vehicleId,
        CommandContext context, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindVisibleAsync(shift.DriverId, context.Scope, cancellationToken);
        if (driver is null) return OperationResult<ShiftCard>.Failed(OperationOutcome.NotFound);
        var changes = new DriverChanges(default, default, default, default, default,
            Optional<string>.Of(vehicleId), default, default, null);
        var result = await updateDrivers.HandleAsync(new UpdateDriverCommand(driver.DriverId, driver.Version, changes, context), cancellationToken);
        if (!result.IsSuccess) return OperationResult<ShiftCard>.FailureOf(result);
        var current = await shifts.FindVisibleAsync(shift.ShiftId, context.Scope, cancellationToken);
        return current is null ? OperationResult<ShiftCard>.Failed(OperationOutcome.NotFound)
            : OperationResult<ShiftCard>.Succeeded(await cards.ComposeAsync(current, cancellationToken));
    }
}
