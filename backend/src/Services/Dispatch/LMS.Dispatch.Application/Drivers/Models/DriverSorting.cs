namespace LMS.Dispatch.Application.Drivers.Models;

public static class DriverSorting
{
    public static IReadOnlyList<string> Fields { get; } =
        ["name", "driverId", "licenseNumber", "phone", "propertyId", "assignedVehicle", "availability", "status", "restriction.reason", "restriction.effectiveDate"];

    public const string DefaultSort = "name,driverId";
    public const string StableField = "driverId";
}
