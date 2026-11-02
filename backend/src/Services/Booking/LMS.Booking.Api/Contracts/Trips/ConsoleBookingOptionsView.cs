namespace LMS.Booking.Api.Contracts.Trips;

public sealed record ConsoleBookingOptionsView(
    IReadOnlyList<ConsoleBookingProductView> Products,
    IReadOnlyList<ConsoleBookingVehicleView> Vehicles,
    bool IsAsapAvailable);
