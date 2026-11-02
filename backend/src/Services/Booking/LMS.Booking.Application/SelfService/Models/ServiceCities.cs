using LMS.Booking.Application.Trips.Services;

namespace LMS.Booking.Application.SelfService.Models;

public static class ServiceCities
{
    public static ServiceCity Of(string region) => CrossBorderRule.IsHongKong(region) ? ServiceCity.HongKong : ServiceCity.Macau;
}
