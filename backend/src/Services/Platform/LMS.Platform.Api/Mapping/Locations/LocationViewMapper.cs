using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Api.Contracts.Locations;
using LMS.Platform.Application.Locations.Models;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Api.Mapping.Locations;

internal static class LocationViewMapper
{
    public static LocationView ToView(CommonLocation location) => new(
        location.LocationId,
        location.Name,
        location.Address,
        new CoordinatesView(location.Coordinates.Latitude, location.Coordinates.Longitude),
        WireEnum.ToWire(location.Region),
        location.CategoryCode,
        location.ContactPerson,
        location.ContactPhone,
        location.OperatingHours,
        new GeofenceView(location.Geofence.IsEnabled, location.Geofence.RadiusMeters),
        location.IsActive,
        location.Notes,
        location.SortOrder,
        location.Version);

    public static LocationOrderingView ToView(LocationOrdering ordering) => new(
        WireEnum.ToWire(ordering.Region),
        ordering.Items.Select(item => new LocationOrderResultView(item.LocationId, item.SortOrder, item.Version)).ToList());
}
