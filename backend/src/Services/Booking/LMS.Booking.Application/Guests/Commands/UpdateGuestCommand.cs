using LMS.BuildingBlocks.Core.Patching;
using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Application.Guests.Commands;

public sealed record UpdateGuestCommand(
    string GuestId,
    Optional<string> Name,
    Optional<string> Tier,
    Optional<string> RoomNumber,
    Optional<string> PropertyId,
    CommandContext Context)
{
    public IReadOnlyList<string> ChangedFields => new[]
    {
        Name.IsSet ? "name" : null,
        Tier.IsSet ? "tier" : null,
        RoomNumber.IsSet ? "roomNumber" : null,
        PropertyId.IsSet ? "propertyId" : null
    }.OfType<string>().ToArray();
}
