namespace LMS.Dispatch.Api.Contracts.Internal;

public sealed record CouponRedemptionView(string RedemptionKey, string CouponId, int RedeemedCount, bool Restored);

public sealed record RedeemCouponRequest(string? RedemptionKey, string? CouponId, string? MemberId, string? GuestId, string? PropertyId, string? VehicleClass);
