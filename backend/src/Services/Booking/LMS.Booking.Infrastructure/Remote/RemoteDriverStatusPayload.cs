namespace LMS.Booking.Infrastructure.Remote;

internal sealed record RemoteDriverStatusPayload(string DriverId, string PropertyId, string Status, string? RestrictionReason);
