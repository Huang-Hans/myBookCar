using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Services;

public static class CustomerDriverContactRules
{
    public static bool CanContact(Trip trip) => trip.Assignment is not null && !trip.IsTerminal;
}
