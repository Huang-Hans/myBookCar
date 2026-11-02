using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Quotas.Models;
using LMS.Booking.Domain.Quotas;

namespace LMS.Booking.Application.Quotas.Ports;

public interface IPropertyQuotaRepository
{
    Task<PropertyQuota?> FindByIdAsync(string propertyId, CancellationToken cancellationToken);

    // 只返回对调用者物业范围可见的限额；不可见与不存在不作区分。
    Task<PropertyQuota?> FindVisibleAsync(string propertyId, PropertyScope scope, CancellationToken cancellationToken);

    Task<PropertyQuota?> FindVisibleAsync(string propertyId, PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken);

    Task<PageResult<PropertyQuota>> ListAsync(ListPropertyQuotasQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<PropertyQuota>> ListVisibleAsync(PropertyScope scope, CancellationToken cancellationToken);

    // 以期望版本为条件替换；版本已变化或记录不存在时返回 false。已用量不会被写入。
    Task<bool> TryReplaceAsync(PropertyQuota updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);
}
