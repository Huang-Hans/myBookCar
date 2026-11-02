namespace LMS.Dispatch.Application.Coupons.Models;

public sealed record CouponRedemptionResult(string RedemptionKey, string CouponId, int RedeemedCount, bool Restored);
