using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Domain.Trips;
using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Application.Trips.Services;

// Shared route rules consume catalog results without choosing an authentication channel.
internal static class ServiceProductRouteRules
{
    private const string LocalScope = "LOCAL";

    public static OperationResult<CheckedRoute> Check(
        ServiceProductOption product,
        DirectoryResult<CatalogLocation> pickup,
        DirectoryResult<CatalogLocation> dropoff)
    {
        var failure = LocationFailure(pickup, "pickupLocationId") ?? LocationFailure(dropoff, "dropoffLocationId");

        return failure ?? ScopedRoute(product, pickup.Value!, dropoff.Value!);
    }

    private static OperationResult<CheckedRoute>? LocationFailure(DirectoryResult<CatalogLocation> location, string field) =>
        location.Outcome switch
        {
            DirectoryOutcome.Success when location.Value!.IsActive => null,
            DirectoryOutcome.Success => OperationResult<CheckedRoute>.Invalid([new FieldIssue(field, "Location is not active")]),
            DirectoryOutcome.NotFound => OperationResult<CheckedRoute>.Invalid([new FieldIssue(field, "Unknown location")]),
            DirectoryOutcome.Forbidden => OperationResult<CheckedRoute>.Failed(OperationOutcome.Forbidden),
            _ => OperationResult<CheckedRoute>.Failed(OperationOutcome.SourceUnavailable)
        };

    private static OperationResult<CheckedRoute> ScopedRoute(ServiceProductOption product, CatalogLocation pickup, CatalogLocation dropoff)
    {
        var isCrossBorder = CrossBorderRule.IsCrossBorder(pickup.Region, dropoff.Region);
        var sameCity = ServiceCities.Of(pickup.Region) == ServiceCities.Of(dropoff.Region);
        var matches = product.RouteScope == LocalScope ? sameCity : !sameCity;
        if (!matches)
        {
            return OperationResult<CheckedRoute>.Rejected([new FieldIssue("productCode", "The route is not served by the selected service")]);
        }

        return OperationResult<CheckedRoute>.Succeeded(
            new CheckedRoute(
                new TripRoute(pickup.LocationId, pickup.Name, dropoff.LocationId, dropoff.Name, isCrossBorder),
                ServiceCities.Of(pickup.Region),
                ServiceCities.Of(dropoff.Region)));
    }
}
