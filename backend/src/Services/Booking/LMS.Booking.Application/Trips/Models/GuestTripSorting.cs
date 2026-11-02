namespace LMS.Booking.Application.Trips.Models;

public static class GuestTripSorting
{
    public static IReadOnlyList<string> Fields { get; } = ["createdAt", "pickupAt", "closedAt", "tripReference", "tripId"];

    public const string DefaultSort = "-createdAt,tripId";
    public const string StableField = "tripId";
}
