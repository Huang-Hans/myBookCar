using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Quotas.Models;
using LMS.Booking.Application.Quotas.Ports;
using LMS.Booking.Domain.Quotas;

namespace LMS.Booking.Application.Quotas.Handlers;

internal sealed class PropertyQuotaQueryHandler(IPropertyQuotaRepository quotas) : IPropertyQuotaQueryHandler
{
    public async Task<OperationResult<PageResult<PropertyQuota>>> ListAsync(
        ListPropertyQuotasQuery query,
        CancellationToken cancellationToken)
    {
        if (query.PropertyId is not null && !query.Scope.Allows(query.PropertyId))
        {
            return OperationResult<PageResult<PropertyQuota>>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<PageResult<PropertyQuota>>.Succeeded(await quotas.ListAsync(query, cancellationToken));
    }

    public async Task<OperationResult<PropertyQuota>> GetAsync(string propertyId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var quota = await quotas.FindVisibleAsync(propertyId, scope, cancellationToken);

        return quota is null
            ? OperationResult<PropertyQuota>.Failed(OperationOutcome.NotFound)
            : OperationResult<PropertyQuota>.Succeeded(quota);
    }

    public async Task<IReadOnlyList<DailyQuotaInfo>> ListDailyQuotasAsync(PropertyScope scope, CancellationToken cancellationToken)
    {
        var visible = await quotas.ListVisibleAsync(scope, cancellationToken);

        return visible.Select(quota => new DailyQuotaInfo(quota.PropertyId, quota.DailyQuota)).ToList();
    }
}
