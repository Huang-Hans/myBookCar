using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Approvals.Models;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Handlers;

public interface IApprovalQueryHandler
{
    Task<OperationResult<PageResult<CrossBorderApproval>>> ListAsync(ListApprovalsQuery query, CancellationToken cancellationToken);

    Task<OperationResult<CrossBorderApproval>> GetAsync(string approvalId, PropertyScope scope, CancellationToken cancellationToken);

    Task<OperationResult<ApprovalSummary>> SummarizeAsync(ListApprovalsQuery query, CancellationToken cancellationToken);
}
