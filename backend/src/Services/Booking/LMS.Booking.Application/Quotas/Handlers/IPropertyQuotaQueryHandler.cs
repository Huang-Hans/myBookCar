using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Quotas.Models;
using LMS.Booking.Domain.Quotas;

namespace LMS.Booking.Application.Quotas.Handlers;

public interface IPropertyQuotaQueryHandler
{
    Task<OperationResult<PageResult<PropertyQuota>>> ListAsync(ListPropertyQuotasQuery query, CancellationToken cancellationToken);

    Task<OperationResult<PropertyQuota>> GetAsync(string propertyId, PropertyScope scope, CancellationToken cancellationToken);

    Task<IReadOnlyList<DailyQuotaInfo>> ListDailyQuotasAsync(PropertyScope scope, CancellationToken cancellationToken);
}
