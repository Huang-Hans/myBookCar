namespace LMS.Booking.Api.Contracts.Trips;

public sealed record ConsoleBookingProductView(
    string ProductCode, string Name, bool IsBookable, string ServiceType, string RouteScope, string? UnavailableReason, string VehicleClass);
