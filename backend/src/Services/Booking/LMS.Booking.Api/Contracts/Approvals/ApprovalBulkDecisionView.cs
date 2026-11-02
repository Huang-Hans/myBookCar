namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 批量决定跨境审批的响应体：逐条给出本次成功的决定结果与提交后的版本。
/// </summary>
/// <param name="Items">本次成功的逐条决定结果；条目级失败不进入列表，而以 items[n].xxx 的字段路径返回校验错误。</param>
/// <param name="Decision">本次统一采用的决定值，APPROVED 或 REJECTED。</param>
public sealed record ApprovalBulkDecisionView(IReadOnlyList<ApprovalDecisionResultView> Items, string Decision);
