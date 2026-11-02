using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Domain.Sources;

namespace LMS.Reporting.Domain.Building;

// 行程明细：每个行程一行。实际时间取接到客人的时间；时长为接到客人到完成；里程为同一区间的轨迹距离。
// 兑换标识没有来源时为空，权益说明目前没有来源，一律为空。
public static class TripDetailRows
{
    public const string LoyaltySource = "Loyalty system · Reference only";

    public static IEnumerable<ReportRow> Build(ReportBuildContext context) =>
        context.Trips.Select(trip => context.Row($"trip:{trip.TripId}", ReportType.TripDetail, trip.PropertyId, trip.ServiceType, PayloadOf(trip)));

    private static TripDetailPayload PayloadOf(ReportTripFact trip) => new(
        trip.TripReference,
        trip.PropertyId,
        trip.GuestName,
        trip.PickupAt,
        trip.PickedUpAt,
        trip.DriverId,
        trip.DriverName,
        trip.VehicleLabel,
        trip.PickupToCompleteMeters,
        DurationSecondsOf(trip),
        new LoyaltyReference(trip.PointsReference, trip.PointsRequired, null, trip.PointsState),
        LoyaltySource);

    private static long? DurationSecondsOf(ReportTripFact trip) =>
        trip.PickedUpAt is null || trip.CompletedAt is null || trip.CompletedAt < trip.PickedUpAt
            ? null
            : (long)(trip.CompletedAt.Value - trip.PickedUpAt.Value).TotalSeconds;
}
