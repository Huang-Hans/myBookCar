using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Coupons.Models;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Application.Coupons.Ports;

public interface ICouponRepository
{
    Task<Coupon?> FindByIdAsync(string couponId, CancellationToken cancellationToken);

    Task<Coupon?> FindByIdAsync(string couponId, ITransactionContext context, CancellationToken cancellationToken);

    Task<Coupon?> FindVisibleAsync(string couponId, PropertyScope scope, CancellationToken cancellationToken);

    Task<Coupon?> FindVisibleAsync(string couponId, PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken);

    Task<Coupon?> FindByCodeAsync(string code, CancellationToken cancellationToken);

    Task<Coupon?> FindByCodeAsync(string code, ITransactionContext context, CancellationToken cancellationToken);

    Task<PageResult<Coupon>> ListAsync(ListCouponsQuery query, CancellationToken cancellationToken);

    Task<CouponSummaryCounts> SummarizeAsync(PropertyScope scope, DateOnly today, CancellationToken cancellationToken);

    // 券碼（不區分大小寫）重複時拋出 DuplicateRecordException。
    Task InsertAsync(Coupon coupon, ITransactionContext context, CancellationToken cancellationToken);

    Task<bool> TryReplaceAsync(Coupon updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string couponId, ITransactionContext context, CancellationToken cancellationToken);
}
