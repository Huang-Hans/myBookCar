namespace LMS.Booking.Application.Trips.Models;

public static class DriverTripSorting
{
    public static IReadOnlyList<string> Fields { get; } = ["closedAt", "pickupAt", "createdAt", "tripReference", "tripId"];

    public const string DefaultSort = "-closedAt,-pickupAt,tripId";
    public const string StableField = "tripId";
}
