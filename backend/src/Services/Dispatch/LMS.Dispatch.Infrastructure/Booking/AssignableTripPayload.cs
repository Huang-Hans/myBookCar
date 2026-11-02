namespace LMS.Dispatch.Infrastructure.Booking;

internal sealed record AssignableTripPayload(
    string TripId,
    string TripReference,
    string PropertyId,
    bool IsAssignable,
    DateTimeOffset PickupAt,
    string PickupName,
    string DropoffName,
    int Passengers,
    string VehicleClass);
