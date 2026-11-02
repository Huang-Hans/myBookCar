namespace LMS.Booking.Api.Contracts.ReportingSources;

/// <summary>
/// 积分引用。LMS 只记录引用，不执行扣减或退回。
/// </summary>
/// <param name="Required">所需积分。</param>
/// <param name="Reference">外部兑换引用；没有时为 null。</param>
/// <param name="State">RESERVED / REDEEMED / RETURNED / NONE。</param>
public sealed record ReportingTripPointsView(long Required, string? Reference, string State);
