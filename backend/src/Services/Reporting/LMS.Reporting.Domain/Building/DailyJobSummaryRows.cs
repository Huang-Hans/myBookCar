using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Domain.Sources;

namespace LMS.Reporting.Domain.Building;

// 日报汇总：每个“物业 × 服务类型”一行。预约数含已取消；未到场是取消原因属于未到场代码的取消，不再计入取消数。
public static class DailyJobSummaryRows
{
    public static IEnumerable<ReportRow> Build(ReportBuildContext context) =>
        context.Trips
            .GroupBy(trip => (trip.PropertyId, trip.ServiceType))
            .Select(group => context.Row(
                $"daily:{context.DayKey}:{group.Key.PropertyId}:{group.Key.ServiceType}",
                ReportType.DailyJobSummary,
                group.Key.PropertyId,
                group.Key.ServiceType,
                PayloadOf(group.Key.PropertyId, group.ToList(), context.NoShowReasonCodes)));

    private static DailyJobSummaryPayload PayloadOf(string propertyId, IReadOnlyList<ReportTripFact> trips, IReadOnlySet<string> noShowCodes)
    {
        var noShows = trips.Count(trip => trip.IsCancelledBy(noShowCodes));
        var cancelled = trips.Count(trip => trip.Outcome == TripOutcome.Cancelled);

        var tally = new RedemptionTally(
            trips.Count(trip => trip.PointsState == "RESERVED"),
            trips.Count(trip => trip.PointsState == "REDEEMED"),
            trips.Count(trip => trip.PointsState == "RETURNED"));

        return new DailyJobSummaryPayload(propertyId, trips.Count, trips.Count(trip => trip.IsCompleted), cancelled - noShows, noShows, tally.Summary, tally);
    }
}
