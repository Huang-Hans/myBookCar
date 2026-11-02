using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Coupons.Models;
using LMS.Dispatch.Application.Coupons.Ports;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Application.Coupons.Handlers;

internal sealed class CouponQueryHandler(ICouponRepository coupons, BusinessCalendar calendar) : ICouponQueryHandler
{
    public async Task<OperationResult<PageResult<CouponCard>>> ListAsync(ListCouponsQuery query, CancellationToken cancellationToken)
    {
        var today = calendar.Today();
        var page = await coupons.ListAsync(query with { Today = today }, cancellationToken);

        return OperationResult<PageResult<CouponCard>>.Succeeded(
            new PageResult<CouponCard>(page.Items.Select(coupon => new CouponCard(coupon, today)).ToList(), page.TotalItems));
    }

    public async Task<OperationResult<CouponSummaryCounts>> SummarizeAsync(PropertyScope scope, CancellationToken cancellationToken)
    {
        return OperationResult<CouponSummaryCounts>.Succeeded(await coupons.SummarizeAsync(scope, calendar.Today(), cancellationToken));
    }

    public async Task<OperationResult<CouponCard>> GetAsync(string couponId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var coupon = await coupons.FindVisibleAsync(couponId, scope, cancellationToken);

        return coupon is null
            ? OperationResult<CouponCard>.Failed(OperationOutcome.NotFound)
            : OperationResult<CouponCard>.Succeeded(new CouponCard(coupon, calendar.Today()));
    }
}
