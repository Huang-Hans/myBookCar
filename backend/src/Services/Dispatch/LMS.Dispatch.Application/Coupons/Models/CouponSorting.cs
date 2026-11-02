namespace LMS.Dispatch.Application.Coupons.Models;

public static class CouponSorting
{
    public static IReadOnlyList<string> Fields { get; } =
        ["code", "name", "memberId", "type", "validFrom", "validTo", "issuedQuantity", "redeemedCount", "couponId"];

    public const string DefaultSort = "code,couponId";
    public const string StableField = "couponId";
}
