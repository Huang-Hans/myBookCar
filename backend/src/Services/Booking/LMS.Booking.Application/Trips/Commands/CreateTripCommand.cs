using System.Text.Json;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Commands;

public sealed record CreateTripCommand(
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
    string? SpecialRequests,
    string ConfirmationLanguage,
    string IdempotencyKey,
    CommandContext Context,
    string? CouponId = null,
    ServiceProduct? Product = null)
{
    public string ToCanonicalForm()
    {
        object?[] values =
        {
            GuestId, PropertyId, PickupLocationId, DropoffLocationId, PickupAt?.ToString("O"),
            PickupMode, ServiceType, VehicleClass, Passengers, Luggage, SpecialRequests, ConfirmationLanguage, CouponId
        };
        return JsonSerializer.Serialize(Product is null ? values : [.. values, Product]);
    }
}
