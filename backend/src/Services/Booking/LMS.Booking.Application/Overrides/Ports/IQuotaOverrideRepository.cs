using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Overrides.Models;
using LMS.Booking.Domain.Overrides;

namespace LMS.Booking.Application.Overrides.Ports;

public interface IQuotaOverrideRepository
{
    Task<QuotaOverride?> FindByIdAsync(string overrideId, CancellationToken cancellationToken);

    Task<QuotaOverride?> FindVisibleAsync(string overrideId, PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken);

    Task<PageResult<QuotaOverride>> ListAsync(ListQuotaOverridesQuery query, CancellationToken cancellationToken);

    // 同物业、同客人已有待审批申请时抛出 DuplicateRecordException。
    Task InsertAsync(QuotaOverride quotaOverride, ITransactionContext context, CancellationToken cancellationToken);

    Task<bool> TryReplaceAsync(QuotaOverride updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);
}
