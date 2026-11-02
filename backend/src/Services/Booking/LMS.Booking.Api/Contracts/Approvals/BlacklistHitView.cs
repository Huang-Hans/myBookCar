namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 黑名单命中项：实时比对得到的限制提示，没有独立的命中集合。
/// </summary>
/// <param name="ApprovalId">命中的审核队列标识。</param>
/// <param name="TripReference">关联的外部行程引用。</param>
/// <param name="TargetType">命中对象类型，CUSTOMER 或 DRIVER。</param>
/// <param name="TargetId">命中对象标识，即 memberId 或 driverId。</param>
/// <param name="Reason">限制说明；没有说明时为空并省略该键。</param>
/// <param name="PropertyId">审核所属物业代码。</param>
public sealed record BlacklistHitView(string ApprovalId, string TripReference, string TargetType, string TargetId, string? Reason, string PropertyId);
