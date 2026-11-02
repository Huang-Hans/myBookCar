using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Vehicles.Ports;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Services;

internal sealed class DriverVehicleBinding(IVehicleRepository vehicles)
{
    public async Task<OperationResult<Driver>> ResolveAsync(Driver driver, PropertyScope scope, CancellationToken cancellationToken)
    {
        if (driver.AssignedVehicleId is null) return OperationResult<Driver>.Succeeded(driver);
        var vehicle = await vehicles.FindVisibleAsync(driver.AssignedVehicleId, scope, cancellationToken);
        if (vehicle is null || vehicle.PropertyId != driver.PropertyId)
            return OperationResult<Driver>.Invalid([new("assignedVehicleId", "The vehicle and driver must belong to the same property.")]);
        if (vehicle.DriverId is not null && vehicle.DriverId != driver.DriverId)
            return OperationResult<Driver>.Invalid([new("assignedVehicleId", "The vehicle is already bound to another driver.")]);
        return OperationResult<Driver>.Succeeded(driver with { AssignedVehicle = $"{vehicle.Plate} · {vehicle.Model}" });
    }
}
