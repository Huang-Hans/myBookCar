using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Domain.Sources;

// 来自 Booking 的一条行程事实（只读来源，不由 Reporting 创造）。司机与车辆是指派时的快照；距离由 Booking 按行驶轨迹算出，轨迹不足时为 null。
public sealed record ReportTripFact(
    string TripId,
    string TripReference,
    string PropertyId,
    string GuestId,
    string GuestName,
    DateTimeOffset PickupAt,
    TripOutcome Outcome,
    ServiceType ServiceType,
    long PointsRequired,
    string? PointsReference,
    string PointsState,
    string? DriverId,
    string? DriverName,
    string? VehicleLabel,
    DateTimeOffset? EnRouteAt,
    DateTimeOffset? PickedUpAt,
    DateTimeOffset? CompletedAt,
    ReportTripCancellation? Cancellation,
    double? PickupToCompleteMeters,
    double? EnRouteToCompleteMeters)
{
    public bool IsCompleted => Outcome == TripOutcome.Completed;

    public bool IsCancelledBy(IReadOnlySet<string> reasonCodes) =>
        Outcome == TripOutcome.Cancelled && Cancellation is not null && reasonCodes.Contains(Cancellation.ReasonCode);
}
