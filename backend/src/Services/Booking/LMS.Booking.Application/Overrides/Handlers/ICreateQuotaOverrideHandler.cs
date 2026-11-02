using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Overrides.Commands;
using LMS.Booking.Domain.Overrides;

namespace LMS.Booking.Application.Overrides.Handlers;

public interface ICreateQuotaOverrideHandler
{
    Task<OperationResult<QuotaOverride>> HandleAsync(CreateQuotaOverrideCommand command, CancellationToken cancellationToken);
}
