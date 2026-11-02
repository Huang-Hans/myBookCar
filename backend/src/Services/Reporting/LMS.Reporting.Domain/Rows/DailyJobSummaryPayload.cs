namespace LMS.Reporting.Domain.Rows;

public sealed record DailyJobSummaryPayload(
    string PropertyId,
    int Bookings,
    int Completed,
    int Cancellations,
    int NoShows,
    string? RedemptionSummary,
    RedemptionTally? Redemption = null) : ReportPayload;
