namespace LMS.Booking.Domain.Approvals;

// 跨境审核状态。对外字面值为 PENDING / APPROVED / REJECTED。
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}
