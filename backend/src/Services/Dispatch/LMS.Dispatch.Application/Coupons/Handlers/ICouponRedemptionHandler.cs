using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Coupons.Commands;
using LMS.Dispatch.Application.Coupons.Models;

namespace LMS.Dispatch.Application.Coupons.Handlers;

public interface ICouponRedemptionHandler
{
    Task<OperationResult<CouponRedemptionResult>> RedeemAsync(RedeemCouponCommand command, CancellationToken cancellationToken);

    Task<OperationResult<CouponRedemptionResult>> RestoreAsync(string redemptionKey, CancellationToken cancellationToken);
}
