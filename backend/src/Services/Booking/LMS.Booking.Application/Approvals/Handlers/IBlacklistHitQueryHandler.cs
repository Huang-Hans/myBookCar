using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Approvals.Models;

namespace LMS.Booking.Application.Approvals.Handlers;

public interface IBlacklistHitQueryHandler
{
    Task<OperationResult<IReadOnlyList<BlacklistHit>>> HandleAsync(PropertyScope scope, string? propertyId, CancellationToken cancellationToken);
}
