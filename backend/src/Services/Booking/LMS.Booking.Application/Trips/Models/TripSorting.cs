namespace LMS.Booking.Application.Trips.Models;

public static class TripSorting
{
    public static IReadOnlyList<string> Fields { get; } =
    [
        "tripReference", "pickupAt", "status", "serviceType", "vehicleClass", "propertyId", "createdAt",
        "guest.name", "guest.roomNumber", "guest.memberId", "tripId"
    ];

    public const string DefaultSort = "-pickupAt,tripId";
    public const string StableField = "tripId";
}
