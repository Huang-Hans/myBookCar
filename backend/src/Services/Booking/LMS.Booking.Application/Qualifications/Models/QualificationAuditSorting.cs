namespace LMS.Booking.Application.Qualifications.Models;

public static class QualificationAuditSorting
{
    public static IReadOnlyList<string> Fields { get; } =
        ["actor", "occurredAt", "qualification.guestId", "qualification.justification", "qualification.status", "auditId"];

    public const string DefaultSort = "occurredAt,auditId";
    public const string StableField = "auditId";
}
