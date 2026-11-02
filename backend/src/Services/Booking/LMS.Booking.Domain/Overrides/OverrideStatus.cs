namespace LMS.Booking.Domain.Overrides;

// 配额例外申请状态。对外字面值为 MANAGER_APPROVAL_REQUIRED / APPROVED / REJECTED。
public enum OverrideStatus
{
    ManagerApprovalRequired,
    Approved,
    Rejected
}
