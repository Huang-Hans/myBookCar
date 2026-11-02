namespace LMS.Dispatch.Application.Offers.Models;

public static class DispatchOfferSorting
{
    public static IReadOnlyList<string> Fields { get; } = ["round", "createdAt", "offerId"];

    // 最近一轮排在最前。
    public const string DefaultSort = "-createdAt,offerId";
    public const string StableField = "offerId";
}
