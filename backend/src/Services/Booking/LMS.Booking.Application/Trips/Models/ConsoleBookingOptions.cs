using LMS.Booking.Application.Console.Options;
using LMS.Booking.Application.SelfService.Options;

namespace LMS.Booking.Application.Trips.Models;

public sealed record ConsoleBookingOptions(
    IReadOnlyList<ServiceProductOption> Products,
    IReadOnlyList<VehicleClassOption> Vehicles,
    bool IsAsapAvailable);
