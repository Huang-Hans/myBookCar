namespace LMS.Booking.Application.Approvals.Models;

public static class ApprovalSorting
{
    public static IReadOnlyList<string> Fields { get; } =
    [
        "tripReference", "guest.name", "guest.tierCode", "propertyId", "route.pickupName", "route.dropoffName",
        "pickupAt", "slaDeadlineAt", "status", "approvalId"
    ];

    public const string DefaultSort = "pickupAt,approvalId";
    public const string StableField = "approvalId";
}
