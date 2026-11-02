using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Quotas.Commands;
using LMS.Booking.Domain.Quotas;

namespace LMS.Booking.Application.Quotas.Handlers;

public interface IUpdatePropertyQuotaHandler
{
    Task<OperationResult<PropertyQuota>> HandleAsync(UpdatePropertyQuotaCommand command, CancellationToken cancellationToken);
}
