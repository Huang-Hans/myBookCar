namespace LMS.Dispatch.Application.Offers.Models;

public static class OfferSorting
{
    public static IReadOnlyList<string> Fields { get; } = ["expiresAt", "createdAt", "offerId"];

    public const string DefaultSort = "expiresAt,offerId";
    public const string StableField = "offerId";
}
