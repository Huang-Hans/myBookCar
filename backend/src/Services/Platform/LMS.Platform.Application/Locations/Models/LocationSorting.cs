namespace LMS.Platform.Application.Locations.Models;

public static class LocationSorting
{
    public static IReadOnlyList<string> Fields { get; } =
    [
        "name", "region", "categoryCode", "address", "coordinates.latitude", "coordinates.longitude", "contactPerson",
        "contactPhone", "operatingHours", "geofence.isEnabled", "isActive", "sortOrder", "locationId"
    ];

    public const string DefaultSort = "region,sortOrder,locationId";
    public const string StableField = "locationId";
}
