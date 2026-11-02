using LMS.Booking.Application.Directory;

namespace LMS.Booking.Application.Coupons;

public sealed record CouponUse(string RedemptionKey, string CouponId, string? MemberId, string? GuestId, string PropertyId, string VehicleClass);

// Dispatch 持有優惠券已使用次數。建立優惠券訂單時核銷一次，取消時退還。
public interface ICouponRedemptions
{
    Task<DirectoryResult<bool>> RedeemAsync(CouponUse use, CancellationToken cancellationToken);

    Task<DirectoryResult<bool>> RestoreAsync(string redemptionKey, CancellationToken cancellationToken);
}
