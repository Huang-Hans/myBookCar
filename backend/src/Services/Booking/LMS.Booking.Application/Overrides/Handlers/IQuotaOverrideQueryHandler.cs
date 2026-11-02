using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.Overrides.Models;
using LMS.Booking.Domain.Overrides;

namespace LMS.Booking.Application.Overrides.Handlers;

public interface IQuotaOverrideQueryHandler
{
    Task<OperationResult<PageResult<QuotaOverride>>> ListAsync(ListQuotaOverridesQuery query, CancellationToken cancellationToken);
}
