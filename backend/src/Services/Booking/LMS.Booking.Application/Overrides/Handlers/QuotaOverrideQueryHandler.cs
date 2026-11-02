using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.Overrides.Models;
using LMS.Booking.Application.Overrides.Ports;
using LMS.Booking.Domain.Overrides;

namespace LMS.Booking.Application.Overrides.Handlers;

internal sealed class QuotaOverrideQueryHandler(IQuotaOverrideRepository overrides) : IQuotaOverrideQueryHandler
{
    public async Task<OperationResult<PageResult<QuotaOverride>>> ListAsync(
        ListQuotaOverridesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.PropertyId is not null && !query.Scope.Allows(query.PropertyId))
        {
            return OperationResult<PageResult<QuotaOverride>>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<PageResult<QuotaOverride>>.Succeeded(await overrides.ListAsync(query, cancellationToken));
    }
}
