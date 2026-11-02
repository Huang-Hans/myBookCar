namespace LMS.Reporting.Infrastructure.Sources.Payloads;

internal sealed record BookingTripAssignmentPayload(string DriverId, string DriverName, string VehicleLabel);
