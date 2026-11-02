using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Domain.Guests;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Models;

// 自助建单评估通过的产物；订单一律受理为 CONFIRMED。
public sealed record SelfBookingApproval(
    GuestProfile Guest,
    ServiceProductOption Product,
    TripRoute Route,
    DateTimeOffset PickupAt,
    VehicleCapacity Capacity);
