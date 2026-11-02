using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Approvals.Commands;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Handlers;

public interface IUpdateApprovalDocumentHandler
{
    Task<OperationResult<CrossBorderApproval>> HandleAsync(UpdateApprovalDocumentCommand command, CancellationToken cancellationToken);
}
