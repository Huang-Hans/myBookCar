using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Domain.Sources;

namespace LMS.Reporting.Domain.Building;

// 取消报表：每个已取消行程一行。原因名称与退还政策取自 Platform 当前的取消原因清单；清单中找不到时名称退回代码、政策退回取消时的物业政策引用。
public static class CancellationRows
{
    public const string CustomerSource = "CUSTOMER";

    public const string StaffSource = "STAFF";

    public static IEnumerable<ReportRow> Build(ReportBuildContext context) =>
        context.Trips
            .Where(trip => trip.Outcome == TripOutcome.Cancelled && trip.Cancellation is not null)
            .Select(trip => context.Row(
                $"cancellation:{trip.TripId}",
                ReportType.Cancellation,
                trip.PropertyId,
                trip.ServiceType,
                PayloadOf(trip, trip.Cancellation!, context.Reasons.GetValueOrDefault(trip.Cancellation!.ReasonCode))));

    private static CancellationPayload PayloadOf(ReportTripFact trip, ReportTripCancellation cancellation, CancellationReasonInfo? reason) => new(
        cancellation.ReasonCode,
        reason?.Name ?? cancellation.ReasonCode,
        cancellation.CancelledBy == trip.GuestId ? CustomerSource : StaffSource,
        (long)Math.Max(0, (trip.PickupAt - cancellation.CancelledAt).TotalSeconds),
        reason?.PointsReturnPolicyCode ?? cancellation.PolicyReference,
        reason?.PointsReturnReference);
}
