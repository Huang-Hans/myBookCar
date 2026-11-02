namespace LMS.Dispatch.Infrastructure.Booking;

internal sealed record DispatchOrderBookedByPayload(string UserId, string RoleCode, string? Name);
