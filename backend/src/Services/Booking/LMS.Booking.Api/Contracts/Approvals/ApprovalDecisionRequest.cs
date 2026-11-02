namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 批准或拒绝单条跨境审批的请求体。
/// </summary>
/// <param name="Decision">审核决定，取大写下划线字面值 APPROVED 或 REJECTED。</param>
/// <param name="Comments">决定评语，批准与拒绝都必填，去除首尾空白后不能为空。</param>
public sealed record ApprovalDecisionRequest(string? Decision, string? Comments);
