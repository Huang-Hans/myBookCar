namespace LMS.Dispatch.Application.Coupons.Commands;

public sealed record RedeemCouponCommand(
    string RedemptionKey,
    string CouponId,
    string? MemberId,
    string? GuestId,
    string? PropertyId,
    string? VehicleClass);
