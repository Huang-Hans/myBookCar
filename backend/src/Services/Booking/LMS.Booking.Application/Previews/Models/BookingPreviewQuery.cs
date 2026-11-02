using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Previews.Models;

// 预订预览输入：与建单的必要子集含义一致。PickupAt 在 ASAP 时可省略。
public sealed record BookingPreviewQuery(
    string GuestId,
    string PropertyId,
    string PickupLocationId,
    string DropoffLocationId,
    DateTimeOffset? PickupAt,
    PickupMode PickupMode,
    ServiceType ServiceType,
    VehicleClass VehicleClass,
    int Passengers,
    int Luggage,
    CommandContext Context,
    ServiceProduct? Product = null);
