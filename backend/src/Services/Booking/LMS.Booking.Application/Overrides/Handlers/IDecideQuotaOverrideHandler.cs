using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Overrides.Commands;
using LMS.Booking.Domain.Overrides;

namespace LMS.Booking.Application.Overrides.Handlers;

public interface IDecideQuotaOverrideHandler
{
    Task<OperationResult<QuotaOverride>> HandleAsync(DecideQuotaOverrideCommand command, CancellationToken cancellationToken);
}
