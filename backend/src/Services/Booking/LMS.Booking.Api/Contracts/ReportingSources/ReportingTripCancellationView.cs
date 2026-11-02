namespace LMS.Booking.Api.Contracts.ReportingSources;

/// <summary>
/// 取消记录。
/// </summary>
/// <param name="ReasonCode">取消原因代码（Platform 取消原因清单）。</param>
/// <param name="CancelledBy">取消人标识：客户本人取消时等于宾客标识，否则为客服用户标识。</param>
/// <param name="CancelledAt">取消时间（UTC）。</param>
/// <param name="PolicyReference">取消时的物业政策引用；没有时为 null。</param>
public sealed record ReportingTripCancellationView(string ReasonCode, string CancelledBy, DateTime CancelledAt, string? PolicyReference);
