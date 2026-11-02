using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Approvals.Commands;
using LMS.Booking.Application.Approvals.Ports;
using LMS.Booking.Application.Approvals.Services;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Approvals;
using LMS.Booking.Domain.Common;

namespace LMS.Booking.Application.Approvals.Handlers;

// 单笔最终决定（批准前检查四项核验、客户限制、司机 Active；拒绝只需要必填评语）。
// 当前模型只有一个最终决定，不是三阶段工作流（Q06）。已决定的审核再次决定为状态冲突；相同幂等键的重试返回审核当前状态。
internal sealed class DecideApprovalHandler(
    DecisionWorkflow workflow,
    IApprovalRepository approvals,
    ApprovalReviewChecks checks,
    TimeProvider timeProvider) : IDecideApprovalHandler
{
    private const string Operation = "approval.decide";
    private const string ResourceType = "cross-border-approval";

    public async Task<OperationResult<CrossBorderApproval>> HandleAsync(DecideApprovalCommand command, CancellationToken cancellationToken)
    {
        var rejection = await PrecheckAsync(command, cancellationToken);

        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
    }

    // 跨域核对只对仍待审核的批准请求进行；已决定的审核交给工作流处理重放或状态冲突。
    private async Task<OperationResult<CrossBorderApproval>?> PrecheckAsync(DecideApprovalCommand command, CancellationToken cancellationToken)
    {
        if (command.Decision != ReviewDecision.Approved)
        {
            return null;
        }

        var approval = await approvals.FindVisibleAsync(command.ApprovalId, command.Context.Scope, cancellationToken);
        if (approval is null)
        {
            return OperationResult<CrossBorderApproval>.Failed(OperationOutcome.NotFound);
        }

        if (approval.Status != ApprovalStatus.Pending)
        {
            return null;
        }

        var result = await checks.CheckAsync([approval], _ => string.Empty, cancellationToken);

        return result.ToFailure<CrossBorderApproval>();
    }

    private DecisionSpecification<CrossBorderApproval> BuildSpecification(DecideApprovalCommand command) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            new PatchSpecification<CrossBorderApproval>(
                command.ApprovalId,
                command.ExpectedVersion,
                (id, context, token) => approvals.FindVisibleAsync(id, command.Context.Scope, context, token),
                approval => approval.Version,
                (approval, _, _) => Task.FromResult(Decide(approval, command)),
                (updated, expected, context, token) => approvals.TryReplaceAsync(updated, expected, context, token),
                (_, after, now) => AuditRecordFactory.Succeeded(
                    command.Context, Operation, ResourceType, after.ApprovalId, now,
                    $"decision: {WireEnum.ToWire(command.Decision)}", after.PropertyId, null)),
            (id, token) => approvals.FindByIdAsync(id, token));

    private OperationResult<CrossBorderApproval> Decide(CrossBorderApproval current, DecideApprovalCommand command)
    {
        if (current.Status != ApprovalStatus.Pending)
        {
            return OperationResult<CrossBorderApproval>.Failed(OperationOutcome.StateConflict);
        }

        if (command.Decision == ReviewDecision.Approved && !current.AreAllDocumentsVerified)
        {
            return OperationResult<CrossBorderApproval>.Rejected([new FieldIssue("documents", "All four documents must be verified before approval")]);
        }

        return OperationResult<CrossBorderApproval>.Succeeded(
            current.Decide(command.Decision, command.Comments, command.Context.Actor, timeProvider.GetUtcNow()));
    }
}
