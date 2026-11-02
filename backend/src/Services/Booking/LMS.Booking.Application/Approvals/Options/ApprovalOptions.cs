namespace LMS.Booking.Application.Approvals.Options;

// 审批相关的技术上限。批量上限保护单个本地事务的规模，不是业务规则。
public sealed class ApprovalOptions
{
    public const string SectionName = "Approvals";

    public int MaxBulkItems { get; init; } = 100;
}
