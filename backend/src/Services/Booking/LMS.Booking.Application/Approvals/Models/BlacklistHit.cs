namespace LMS.Booking.Application.Approvals.Models;

// 当前审批队列中命中客户限制或司机停用/暂停的记录，实时读取，不持久化。
public sealed record BlacklistHit(string ApprovalId, string TripReference, HitTargetType TargetType, string TargetId, string? Reason, string PropertyId);
