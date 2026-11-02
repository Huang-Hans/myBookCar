namespace LMS.Booking.Application.ReportingSources.Models;

// 一批报表来源行程：计划接车时间落在 [From, To) 内、行程标识大于 AfterTripId 的前 Limit 个。
public sealed record ReportingTripFactsQuery(DateTimeOffset From, DateTimeOffset To, string? AfterTripId, int Limit);
