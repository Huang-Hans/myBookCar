using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Booking.Api.Contracts.Guests;

/// <summary>Partially updates a member snapshot. Member ID and login credentials are read-only.</summary>
public sealed record UpdateGuestRequest(
    Optional<string?> Name,
    Optional<string?> Tier,
    Optional<string?> RoomNumber,
    Optional<string?> PropertyId);
