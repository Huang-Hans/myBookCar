namespace LMS.Booking.Application.Trips.Services;

// 跨境判定（实施基线 D8）：任一端在香港即视为跨境。客服代订与客户自助预订共用同一口径。
internal static class CrossBorderRule
{
    private const string HongKongRegion = "HONG_KONG";

    public static bool IsCrossBorder(string pickupRegion, string dropoffRegion) =>
        IsHongKong(pickupRegion) || IsHongKong(dropoffRegion);

    public static bool IsHongKong(string region) =>
        string.Equals(region, HongKongRegion, StringComparison.OrdinalIgnoreCase);
}
