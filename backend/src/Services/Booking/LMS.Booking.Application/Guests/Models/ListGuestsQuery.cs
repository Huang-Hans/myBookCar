using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Booking.Application.Guests.Models;

public sealed record ListGuestsQuery(ListQuery List, PropertyScope Scope, string? TierCode);

public static class GuestSorting
{
    public static IReadOnlyList<string> Fields { get; } = ["name", "memberId", "roomNumber", "tierCode", "guestId"];

    public const string DefaultSort = "name,guestId";
    public const string StableField = "guestId";
}
