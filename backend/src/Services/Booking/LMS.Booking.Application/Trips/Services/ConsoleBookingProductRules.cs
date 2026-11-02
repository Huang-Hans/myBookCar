using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Trips.Services;

internal sealed class ConsoleBookingProductRules(
    IOptions<SelfServiceOptions> options,
    IPlatformCatalog catalog)
{
    public async Task<OperationResult<ServiceType>> CheckAsync(
        ServiceProduct product, string pickupId, string dropoffId, PickupMode mode, CancellationToken cancellationToken)
    {
        var selection = options.Value.EffectiveProducts.FirstOrDefault(item => item.Code == WireEnum.ToWire(product));
        if (selection is not { IsEnabled: true })
            return OperationResult<ServiceType>.Invalid([new FieldIssue("productCode", "The service product is not available")]);
        if (mode == PickupMode.Asap && !options.Value.AsapEnabled)
            return OperationResult<ServiceType>.Invalid([new FieldIssue("pickupMode", "Immediate pickup is not available; schedule the pickup in advance")]);

        var route = await CheckRouteAsync(selection, pickupId, dropoffId, cancellationToken);
        if (!route.IsSuccess) return OperationResult<ServiceType>.FailureOf(route);

        return WireEnum.TryParse<ServiceType>(selection.ServiceType, out var serviceType)
            ? OperationResult<ServiceType>.Succeeded(serviceType)
            : throw new InvalidOperationException($"Service product '{selection.Code}' maps to an unknown service type.");
    }

    private async Task<OperationResult<CheckedRoute>> CheckRouteAsync(
        ServiceProductOption product, string pickupId, string dropoffId, CancellationToken cancellationToken)
    {
        if (string.Equals(pickupId, dropoffId, StringComparison.Ordinal))
            return OperationResult<CheckedRoute>.Invalid([new FieldIssue("dropoffLocationId", "Must differ from pickupLocationId")]);

        var pickup = await catalog.GetLocationAsync(pickupId, cancellationToken);
        var dropoff = await catalog.GetLocationAsync(dropoffId, cancellationToken);

        return ServiceProductRouteRules.Check(product, pickup, dropoff);
    }
}
