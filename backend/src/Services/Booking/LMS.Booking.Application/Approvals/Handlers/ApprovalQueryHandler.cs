using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Approvals.Models;
using LMS.Booking.Application.Approvals.Ports;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Handlers;

internal sealed class ApprovalQueryHandler(IApprovalRepository approvals) : IApprovalQueryHandler
{
    public async Task<OperationResult<PageResult<CrossBorderApproval>>> ListAsync(
        ListApprovalsQuery query,
        CancellationToken cancellationToken)
    {
        if (IsOutOfScope(query))
        {
            return OperationResult<PageResult<CrossBorderApproval>>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<PageResult<CrossBorderApproval>>.Succeeded(await approvals.ListAsync(query, cancellationToken));
    }

    public async Task<OperationResult<CrossBorderApproval>> GetAsync(string approvalId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var approval = await approvals.FindVisibleAsync(approvalId, scope, cancellationToken);

        return approval is null
            ? OperationResult<CrossBorderApproval>.Failed(OperationOutcome.NotFound)
            : OperationResult<CrossBorderApproval>.Succeeded(approval);
    }

    public async Task<OperationResult<ApprovalSummary>> SummarizeAsync(ListApprovalsQuery query, CancellationToken cancellationToken)
    {
        if (IsOutOfScope(query))
        {
            return OperationResult<ApprovalSummary>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<ApprovalSummary>.Succeeded(await approvals.SummarizeAsync(query, cancellationToken));
    }

    private static bool IsOutOfScope(ListApprovalsQuery query) => query.PropertyId is not null && !query.Scope.Allows(query.PropertyId);
}
