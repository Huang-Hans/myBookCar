namespace LMS.Booking.Application.Qualifications.Models;

public static class QualificationSorting
{
    public static IReadOnlyList<string> Fields { get; } =
    [
        "guest.name", "guest.guestId", "guest.tierCode", "propertyId", "loyaltyOutcome", "qualificationReference", "status", "qualificationId"
    ];

    public const string DefaultSort = "guest.name,qualificationId";
    public const string StableField = "qualificationId";
}
