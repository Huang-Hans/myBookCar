namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 批量决定中单条审核的决定结果。
/// </summary>
/// <param name="ApprovalId">审核记录标识。</param>
/// <param name="Status">决定后的审核状态，APPROVED 或 REJECTED。</param>
/// <param name="Version">提交后的乐观锁版本号，配合 X-Expected-Version 头做并发控制。</param>
public sealed record ApprovalDecisionResultView(string ApprovalId, string Status, long Version);
