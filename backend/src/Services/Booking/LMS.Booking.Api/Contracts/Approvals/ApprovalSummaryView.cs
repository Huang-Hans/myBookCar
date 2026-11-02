namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 跨境审批汇总卡片：按当前授权范围与筛选给出待审批数量与审批链路。
/// </summary>
/// <param name="PendingCount">当前授权范围及筛选内的待审批条数。</param>
/// <param name="ApprovalChain">原型展示的有序审批阶段标签；不是 roleCode，不用于授权。</param>
// urgentCount 的阈值未确定（Q06），因此不提供该字段。
public sealed record ApprovalSummaryView(int PendingCount, IReadOnlyList<string> ApprovalChain);
