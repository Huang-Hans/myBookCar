using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Approvals.Commands;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Handlers;

public interface IDecideApprovalHandler
{
    Task<OperationResult<CrossBorderApproval>> HandleAsync(DecideApprovalCommand command, CancellationToken cancellationToken);
}
