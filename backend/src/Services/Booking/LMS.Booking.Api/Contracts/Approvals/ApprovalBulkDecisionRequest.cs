namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 批量决定跨境审批的请求体：对显式勾选的多条待审批记录统一给出批准或拒绝。
/// </summary>
/// <param name="Items">本次决定的审核条目；不能为空、同一 approvalId 不得重复，且只针对显式选择的待审批请求。</param>
/// <param name="Decision">统一决定值，取大写下划线字面值 APPROVED 或 REJECTED。</param>
/// <param name="Comments">决定评语，批准与拒绝都必填，去除首尾空白后不能为空。</param>
public sealed record ApprovalBulkDecisionRequest(List<ApprovalVersionItemRequest?>? Items, string? Decision, string? Comments);
