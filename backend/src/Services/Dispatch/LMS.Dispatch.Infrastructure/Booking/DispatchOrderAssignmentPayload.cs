namespace LMS.Dispatch.Infrastructure.Booking;

internal sealed record DispatchOrderAssignmentPayload(string OfferId, string DriverId, string DriverName, string VehicleLabel, DateTimeOffset AssignedAt);
