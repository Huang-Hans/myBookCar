using LMS.Booking.Application.Console.Options;
using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Application.Trips.Models;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Trips.Handlers;

internal sealed class ConsoleBookingOptionsHandler(
    IOptions<SelfServiceOptions> products,
    IOptions<ConsoleOptions> vehicles) : IConsoleBookingOptionsHandler
{
    public ConsoleBookingOptions Get() => new(
        products.Value.EffectiveProducts,
        vehicles.Value.EffectiveVehicleClasses,
        products.Value.AsapEnabled);
}
