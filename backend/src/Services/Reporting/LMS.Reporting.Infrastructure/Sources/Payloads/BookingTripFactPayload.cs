namespace LMS.Reporting.Infrastructure.Sources.Payloads;

// Booking GET internal/v1/reporting/trip-facts 的一条行程事实。
internal sealed record BookingTripFactPayload(
    string TripId,
    string TripReference,
    string PropertyId,
    string GuestId,
    string GuestName,
    DateTimeOffset PickupAt,
    string Status,
    bool IsCrossBorder,
    string? Product,
    BookingTripPointsPayload Points,
    BookingTripAssignmentPayload? Assignment,
    DateTimeOffset? EnRouteAt,
    DateTimeOffset? PickedUpAt,
    DateTimeOffset? CompletedAt,
    BookingTripCancellationPayload? Cancellation,
    double? PickupToCompleteMeters,
    double? EnRouteToCompleteMeters);
