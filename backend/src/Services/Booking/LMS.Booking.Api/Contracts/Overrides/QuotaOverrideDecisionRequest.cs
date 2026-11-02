namespace LMS.Booking.Api.Contracts.Overrides;

/// <summary>
/// 批准或拒绝限额例外的请求体；原型确认框没有额外评语输入，因此不含 comments 字段。
/// </summary>
/// <param name="Decision">决定值，取大写下划线字面值 APPROVED 或 REJECTED；仅经理授权主体可提交。</param>
public sealed record QuotaOverrideDecisionRequest(string? Decision);
