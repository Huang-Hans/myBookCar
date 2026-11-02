using System.Text.Json;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.SelfService.Models;

namespace LMS.Booking.Application.SelfService.Commands;

// 客户自助建单。客户身份在 Booking.GuestId，来自令牌绑定，请求正文不接受任何宾客或经办人字段。
public sealed record CreateSelfServiceTripCommand(
    SelfBookingRequest Booking,
    string? SpecialRequests,
    string? ConfirmationLanguage,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            Booking.GuestId, Booking.ProductCode, Booking.PickupLocationId, Booking.DropoffLocationId, Booking.PickupMode,
            Booking.PickupAt?.ToString("O"), Booking.VehicleClass, Booking.Passengers, Booking.Luggage, SpecialRequests, ConfirmationLanguage
        });
}
