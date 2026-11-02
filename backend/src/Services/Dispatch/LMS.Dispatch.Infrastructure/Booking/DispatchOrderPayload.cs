namespace LMS.Dispatch.Infrastructure.Booking;

// Booking 派单队列订单的响应结构（见 Booking 的 DispatchOrderView）。
internal sealed record DispatchOrderPayload(
    string TripId,
    string TripReference,
    string PropertyId,
    string Channel,
    DispatchOrderGuestPayload Guest,
    DispatchOrderRoutePayload Route,
    DateTimeOffset PickupAt,
    string PickupMode,
    string ServiceType,
    string VehicleClass,
    int Passengers,
    int Luggage,
    string? SpecialRequests,
    string Status,
    DispatchOrderAssignmentPayload? Assignment,
    DispatchOrderBookedByPayload BookedBy,
    DateTimeOffset CreatedAt,
    long Version);
