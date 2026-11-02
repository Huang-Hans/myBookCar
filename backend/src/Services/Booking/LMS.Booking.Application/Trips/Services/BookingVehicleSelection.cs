using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.Trips;
using LMS.Booking.Application.SelfService.Options;
using LMS.BuildingBlocks.Core.Wire;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Trips.Services;

internal sealed class BookingVehicleSelection(IBookingVehicleCatalog catalog, VehicleCapacityResolver capacities, IOptions<SelfServiceOptions> products)
{
    public async Task<OperationResult<string>> CheckAsync(
        string propertyId, VehicleClass vehicleClass, string? model, int passengers, CancellationToken cancellationToken,
        ServiceProduct? product = null)
    {
        if (product is { } value && !IsClassAllowed(value, vehicleClass))
            return OperationResult<string>.Invalid([new FieldIssue("vehicleModel", "The selected vehicle model is not available for this service")]);
        if (model is null) return OperationResult<string>.Succeeded(string.Empty);
        var result = await catalog.ListAsync(propertyId, cancellationToken);
        if (!result.IsSuccess) return OperationResult<string>.Failed(OperationOutcome.SourceUnavailable);
        var selected = result.Value!.FirstOrDefault(item => item.VehicleClass == vehicleClass &&
            string.Equals(item.Model, model.Trim(), StringComparison.OrdinalIgnoreCase));
        if (selected is null)
            return OperationResult<string>.Invalid([new FieldIssue("vehicleModel", "The selected vehicle model is not available for this property and vehicle class")]);
        return passengers <= Math.Min(selected.Seats, capacities.CapacityOf(vehicleClass).MaxPassengers)
            ? OperationResult<string>.Succeeded(selected.Model)
            : OperationResult<string>.Invalid([new FieldIssue("passengers", "Passengers exceed the capacity of the selected vehicle model")]);
    }

    private bool IsClassAllowed(ServiceProduct product, VehicleClass vehicleClass) =>
        products.Value.EffectiveProducts.FirstOrDefault(item => item.Code == WireEnum.ToWire(product))
            ?.AllowsVehicleClass(WireEnum.ToWire(vehicleClass)) == true;
}
