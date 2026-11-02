namespace LMS.Dispatch.Infrastructure.Booking;

internal sealed record DispatchOrderRoutePayload(string PickupName, string DropoffName, bool IsCrossBorder);
