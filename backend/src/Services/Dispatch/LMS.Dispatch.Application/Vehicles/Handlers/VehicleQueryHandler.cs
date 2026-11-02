using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Vehicles.Models;
using LMS.Dispatch.Application.Vehicles.Ports;
using LMS.Dispatch.Domain.Vehicles;

namespace LMS.Dispatch.Application.Vehicles.Handlers;

internal sealed class VehicleQueryHandler(IVehicleRepository vehicles, IDriverRepository drivers) : IVehicleQueryHandler
{
    public async Task<OperationResult<PageResult<VehicleCard>>> ListAsync(ListVehiclesQuery query, CancellationToken cancellationToken)
    {
        var property = ReadProperty(query.PropertyId, query.Scope);
        if (!property.IsSuccess)
        {
            return OperationResult<PageResult<VehicleCard>>.FailureOf(property);
        }

        var page = await vehicles.ListAsync(query with { PropertyId = property.Value }, cancellationToken);
        var names = await DriverNamesAsync(page.Items, cancellationToken);

        return OperationResult<PageResult<VehicleCard>>.Succeeded(
            new PageResult<VehicleCard>(page.Items.Select(vehicle => Card(vehicle, names)).ToList(), page.TotalItems));
    }

    public async Task<OperationResult<VehicleStatusCounts>> SummarizeAsync(VehicleSummaryQuery query, CancellationToken cancellationToken)
    {
        var property = ReadProperty(query.PropertyId, query.Scope);
        if (!property.IsSuccess)
        {
            return OperationResult<VehicleStatusCounts>.FailureOf(property);
        }

        return OperationResult<VehicleStatusCounts>.Succeeded(
            await vehicles.CountByStatusAsync(query.Scope, property.Value, cancellationToken));
    }

    public async Task<OperationResult<VehicleCard>> GetAsync(string vehicleId, VehicleSummaryQuery scope, CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.FindVisibleAsync(vehicleId, scope.Scope, cancellationToken);
        if (vehicle is null)
        {
            return OperationResult<VehicleCard>.Failed(OperationOutcome.NotFound);
        }

        var names = await DriverNamesAsync([vehicle], cancellationToken);

        return OperationResult<VehicleCard>.Succeeded(Card(vehicle, names));
    }

    private async Task<IReadOnlyDictionary<string, string>> DriverNamesAsync(
        IReadOnlyList<Vehicle> vehicles,
        CancellationToken cancellationToken)
    {
        var ids = vehicles.Select(vehicle => vehicle.DriverId).Where(id => id is not null).Cast<string>().Distinct(StringComparer.Ordinal).ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var found = await drivers.FindByIdsAsync(ids, cancellationToken);

        return found.ToDictionary(driver => driver.DriverId, driver => driver.Name, StringComparer.Ordinal);
    }

    private static VehicleCard Card(Vehicle vehicle, IReadOnlyDictionary<string, string> names) =>
        new(vehicle, vehicle.DriverId is not null && names.TryGetValue(vehicle.DriverId, out var name) ? name : null);

    private static OperationResult<string?> ReadProperty(string? propertyId, PropertyScope scope)
    {
        if (propertyId is not null && string.IsNullOrWhiteSpace(propertyId))
        {
            return OperationResult<string?>.Invalid([new FieldIssue("propertyId", "Must not be blank")]);
        }

        var normalized = propertyId?.Trim();
        if (normalized is not null && !scope.Allows(normalized))
        {
            return OperationResult<string?>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<string?>.Succeeded(normalized);
    }
}
