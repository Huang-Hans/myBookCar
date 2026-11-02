namespace LMS.Booking.Domain.Qualifications;

// 资格例外的本地处理状态，与外部资格结果（loyaltyOutcome）分离。
// 对外字面值为 ELIGIBLE / MANAGER_APPROVAL_REQUIRED / ESCALATED / APPROVED / REJECTED。
public enum QualificationStatus
{
    Eligible,
    ManagerApprovalRequired,
    Escalated,
    Approved,
    Rejected
}
