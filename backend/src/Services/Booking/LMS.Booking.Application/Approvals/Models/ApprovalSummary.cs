namespace LMS.Booking.Application.Approvals.Models;

// 待审核数量。紧急数量的阈值未确定（Q06），因此不提供，也不返回虚构的 0。
public sealed record ApprovalSummary(int PendingCount, IReadOnlyList<string> ApprovalChain);
