using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Approvals.Commands;
using LMS.Booking.Application.Approvals.Models;

namespace LMS.Booking.Application.Approvals.Handlers;

public interface IBulkDecideApprovalsHandler
{
    Task<OperationResult<BulkDecisionResult>> HandleAsync(BulkDecideApprovalsCommand command, CancellationToken cancellationToken);
}
