using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Overrides.Commands;
using LMS.Booking.Application.Overrides.Ports;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Overrides;

namespace LMS.Booking.Application.Overrides.Handlers;

// 经理批准或拒绝待审批的例外申请：决定嵌入申请本身，决定与审计在同一事务提交。
// 只记录决定，不修改任何物业限额或权益——批准后的实际生效语义（对象、有效期、是否影响周/月）待 Q05。
internal sealed class DecideQuotaOverrideHandler(
    DecisionWorkflow workflow,
    IQuotaOverrideRepository overrides,
    TimeProvider timeProvider) : IDecideQuotaOverrideHandler
{
    private const string Operation = "quota-override.decide";
    private const string ResourceType = "quota-override";

    public Task<OperationResult<QuotaOverride>> HandleAsync(DecideQuotaOverrideCommand command, CancellationToken cancellationToken) =>
        workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);

    private DecisionSpecification<QuotaOverride> BuildSpecification(DecideQuotaOverrideCommand command) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            new PatchSpecification<QuotaOverride>(
                command.OverrideId,
                command.ExpectedVersion,
                (id, context, token) => overrides.FindVisibleAsync(id, command.Context.Scope, context, token),
                quotaOverride => quotaOverride.Version,
                (quotaOverride, _, _) => Task.FromResult(Decide(quotaOverride, command)),
                (updated, expected, context, token) => overrides.TryReplaceAsync(updated, expected, context, token),
                (_, after, now) => AuditRecordFactory.Succeeded(
                    command.Context, Operation, ResourceType, after.OverrideId, now,
                    $"decision: {WireEnum.ToWire(command.Decision)}", after.PropertyId, null)),
            (id, token) => overrides.FindByIdAsync(id, token));

    private OperationResult<QuotaOverride> Decide(QuotaOverride current, DecideQuotaOverrideCommand command) =>
        current.Status != OverrideStatus.ManagerApprovalRequired
            ? OperationResult<QuotaOverride>.Failed(OperationOutcome.StateConflict)
            : OperationResult<QuotaOverride>.Succeeded(current.Decide(command.Decision, command.Context.Actor, timeProvider.GetUtcNow()));
}
