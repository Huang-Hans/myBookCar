namespace LMS.Booking.Application.ReportingSources.Models;

// 一批行程事实；NextAfterTripId 为空表示已读完，否则作为下一批的游标。
public sealed record ReportingTripFactPage(IReadOnlyList<ReportingTripFact> Items, string? NextAfterTripId);
