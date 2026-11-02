using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Api.Contracts.Customer;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Application.SelfService.Services;

namespace LMS.Booking.Api.Mapping.Customer;

internal static class CustomerCatalogMapper
{
    public static CustomerSummaryView ToView(CustomerSummary summary, SelfCancellationRules rules)
    {
        var guest = summary.Guest;

        return new CustomerSummaryView(
            guest.GuestId,
            guest.Name,
            guest.TierCode,
            guest.MemberId,
            guest.PropertyId,
            guest.PreferredLanguage,
            summary.Block is null,
            summary.Block is { } block ? WireEnum.ToWire(block) : null,
            summary.CurrentTrip is null ? null : CustomerTripMapper.ToSummary(summary.CurrentTrip, rules));
    }

    public static ServiceOfferingsView ToView(ServiceOfferings offerings) => new(
        offerings.Cities.Select(city => new CityOfferingView(WireEnum.ToWire(city.City), CityNameOf(city.City), city.Regions)).ToList(),
        offerings.Products.Select(item => new ProductOfferingView(
            item.Product.Code, item.Product.Name, item.Product.Description, item.Product.RouteScope, item.IsBookable, item.UnavailableReason)).ToList(),
        new BookingRulesView(offerings.Rules.MinimumLeadTimeHours, offerings.Rules.MaximumAdvanceDays, offerings.Rules.MaximumActiveRequestsPerGuestPerDay),
        offerings.IsAsapAvailable);

    public static CustomerLocationView ToView(CatalogLocationDetail location) => new(
        location.LocationId,
        location.Name,
        location.Address,
        WireEnum.ToWire(ServiceCities.Of(location.Region)),
        location.Region,
        location.CategoryCode,
        location.OperatingHours);

    public static CustomerLocationDetailView ToView(CatalogLocationProfile location) => new(
        location.LocationId,
        location.Name,
        location.Address,
        WireEnum.ToWire(ServiceCities.Of(location.Region)),
        location.Region,
        location.CategoryCode,
        location.OperatingHours,
        location.Coordinates is null ? null : new CustomerCoordinatesView(location.Coordinates.Latitude, location.Coordinates.Longitude));

    public static RideOptionsView ToView(RideOptionsResult result) => new(
        new RideRouteView(
            result.Route.Route.PickupLocationId,
            result.Route.Route.PickupName,
            result.Route.Route.DropoffLocationId,
            result.Route.Route.DropoffName,
            result.Route.Route.IsCrossBorder,
            WireEnum.ToWire(result.Route.PickupCity),
            WireEnum.ToWire(result.Route.DropoffCity)),
        result.Options.Select(option => new RideOptionView(
            WireEnum.ToWire(option.VehicleClass), option.MaxPassengers, option.MaxLuggage, option.PointsRequired, option.IsEligible, option.IneligibleReason)).ToList());

    private static string CityNameOf(ServiceCity city) => city == ServiceCity.Macau ? "Macau" : "Hong Kong";
}
