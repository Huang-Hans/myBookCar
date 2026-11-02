using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Services;

internal sealed record QualificationTransition(string Operation, string Summary, QualificationStatus Required, QualificationStatus Target)
{
    public static QualificationTransition Escalation { get; } = new(
        "qualification.escalate", "Exception escalated", QualificationStatus.ManagerApprovalRequired, QualificationStatus.Escalated);

    public static QualificationTransition Approval { get; } = new(
        "qualification.approve", "Exception approved", QualificationStatus.Escalated, QualificationStatus.Approved);

    public static QualificationTransition Rejection { get; } = new(
        "qualification.reject", "Exception rejected", QualificationStatus.Escalated, QualificationStatus.Rejected);
}
