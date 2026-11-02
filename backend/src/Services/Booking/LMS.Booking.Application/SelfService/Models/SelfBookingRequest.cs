using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Models;

// 一次自助建单的评估输入。GuestId 来自令牌绑定；PickupAt 在 ASAP 时为 null，由评估按固定缓冲换算。
public sealed record SelfBookingRequest(
    string GuestId,
    string ProductCode,
    string PickupLocationId,
    string DropoffLocationId,
    PickupMode PickupMode,
    DateTimeOffset? PickupAt,
    VehicleClass VehicleClass,
    int Passengers,
    int Luggage);
