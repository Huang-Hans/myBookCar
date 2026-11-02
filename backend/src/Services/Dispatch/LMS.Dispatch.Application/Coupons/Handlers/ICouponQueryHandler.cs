using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Coupons.Models;

namespace LMS.Dispatch.Application.Coupons.Handlers;

public interface ICouponQueryHandler
{
    Task<OperationResult<PageResult<CouponCard>>> ListAsync(ListCouponsQuery query, CancellationToken cancellationToken);

    Task<OperationResult<CouponSummaryCounts>> SummarizeAsync(PropertyScope scope, CancellationToken cancellationToken);

    Task<OperationResult<CouponCard>> GetAsync(string couponId, PropertyScope scope, CancellationToken cancellationToken);
}
