using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Directory;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Coupons;

// 把優惠券核銷結果轉成行程操作結果。成功時返回 null，呼叫方繼續建立或取消訂單。
internal sealed class CouponLedger(ICouponRedemptions coupons)
{
    public async Task<OperationResult<Trip>?> RedeemAsync(CouponUse use, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(use.MemberId) && string.IsNullOrWhiteSpace(use.GuestId))
        {
            return OperationResult<Trip>.Rejected([new FieldIssue("couponId", "This member has no member ID, so the coupon cannot be used.")]);
        }

        return FailureOf(await coupons.RedeemAsync(use, cancellationToken));
    }

    public async Task<OperationResult<Trip>?> RestoreAsync(string? redemptionKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(redemptionKey))
        {
            return null;
        }

        return FailureOf(await coupons.RestoreAsync(redemptionKey, cancellationToken));
    }

    private static OperationResult<Trip>? FailureOf<T>(DirectoryResult<T> result)
    {
        if (result.IsSuccess)
        {
            return null;
        }

        return result.Outcome switch
        {
            DirectoryOutcome.Rejected => OperationResult<Trip>.Rejected(
                result.Issues.Count > 0 ? result.Issues : [new FieldIssue("couponId", "This coupon cannot be used.")]),
            DirectoryOutcome.NotFound => OperationResult<Trip>.Rejected([new FieldIssue("couponId", "Coupon was not found.")]),
            DirectoryOutcome.Conflict => OperationResult<Trip>.Failed(OperationOutcome.StateConflict),
            DirectoryOutcome.Forbidden => OperationResult<Trip>.Failed(OperationOutcome.Forbidden),
            _ => OperationResult<Trip>.Failed(OperationOutcome.SourceUnavailable)
        };
    }
}
