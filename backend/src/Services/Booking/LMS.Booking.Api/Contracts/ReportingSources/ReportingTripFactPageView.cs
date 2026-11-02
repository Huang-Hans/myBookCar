namespace LMS.Booking.Api.Contracts.ReportingSources;

/// <summary>
/// 一批报表来源行程。
/// </summary>
/// <param name="Items">按行程标识升序的行程事实。</param>
/// <param name="NextAfter">下一批的游标；为 null 表示已读完。</param>
public sealed record ReportingTripFactPageView(IReadOnlyList<ReportingTripFactView> Items, string? NextAfter);
