namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 批量决定中的单条审核条目：定位目标记录并携带其并发条件。
/// </summary>
/// <param name="ApprovalId">审核记录标识；同一批量请求内不得重复。</param>
/// <param name="ExpectedVersion">该审核的当前乐观锁版本号，至少为 1，用于批量逐条并发校验。</param>
public sealed record ApprovalVersionItemRequest(string? ApprovalId, long? ExpectedVersion);
