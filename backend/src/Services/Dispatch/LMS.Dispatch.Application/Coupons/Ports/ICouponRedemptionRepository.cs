using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Application.Coupons.Ports;

public interface ICouponRedemptionRepository
{
    Task<CouponRedemption?> FindAsync(string redemptionKey, CancellationToken cancellationToken);

    Task<CouponRedemption?> FindAsync(string redemptionKey, ITransactionContext context, CancellationToken cancellationToken);

    Task InsertAsync(CouponRedemption redemption, ITransactionContext context, CancellationToken cancellationToken);

    // 僅在目前狀態與目標不同時更新。回傳 false 表示記錄不存在或狀態已經相同。
    Task<bool> TrySetRestoredAsync(string redemptionKey, bool restored, ITransactionContext context, CancellationToken cancellationToken);
}
