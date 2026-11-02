namespace LMS.Dispatch.Application.Vehicles.Models;

public static class VehicleSorting
{
    public static IReadOnlyList<string> Fields { get; } =
        ["plate", "model", "propertyId", "grade", "seats", "mileageKm", "status", "vehicleId"];

    public const string DefaultSort = "plate,vehicleId";
    public const string StableField = "vehicleId";
}
