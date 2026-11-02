using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Quotas.Commands;
using LMS.Booking.Application.Quotas.Ports;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Quotas;

namespace LMS.Booking.Application.Quotas.Handlers;

// 调整日/周/月限额与耗尽策略。合并后必须满足 日 ≤ 周 ≤ 月，且被修改的限额不低于权威已用量；
// 已用量缺失时返回“来源不可用”，绝不把缺失当作 0 而跳过保护。
internal sealed class UpdatePropertyQuotaHandler(PatchWorkflow workflow, IPropertyQuotaRepository quotas) : IUpdatePropertyQuotaHandler
{
    private const string Operation = "property-quota.update";
    private const string ResourceType = "property-quota";

    public Task<OperationResult<PropertyQuota>> HandleAsync(UpdatePropertyQuotaCommand command, CancellationToken cancellationToken) =>
        workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);

    private PatchSpecification<PropertyQuota> BuildSpecification(UpdatePropertyQuotaCommand command) =>
        new(
            command.PropertyId,
            command.ExpectedVersion,
            (id, context, token) => quotas.FindVisibleAsync(id, command.Context.Scope, context, token),
            quota => quota.Version,
            (quota, _, _) => Task.FromResult(Merge(quota, command)),
            (updated, expected, context, token) => quotas.TryReplaceAsync(updated, expected, context, token),
            (_, after, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, after.PropertyId, now, command.Changes.ChangedFields(), after.PropertyId));

    private static OperationResult<PropertyQuota> Merge(PropertyQuota current, UpdatePropertyQuotaCommand command)
    {
        var merged = current.Apply(command.Changes);
        var issues = PropertyQuotaRules.Validate(merged);
        if (issues.Count > 0)
        {
            return OperationResult<PropertyQuota>.Invalid(issues);
        }

        return command.Changes.ChangesLimits ? CheckUsage(current, merged, command.Changes) : OperationResult<PropertyQuota>.Succeeded(merged);
    }

    private static OperationResult<PropertyQuota> CheckUsage(PropertyQuota current, PropertyQuota merged, PropertyQuotaChanges changes)
    {
        if (!current.IsUsageKnown)
        {
            return OperationResult<PropertyQuota>.Failed(OperationOutcome.SourceUnavailable);
        }

        var issues = PropertyQuotaRules.ValidateAgainstUsage(merged, changes);

        return issues.Count > 0 ? OperationResult<PropertyQuota>.Invalid(issues) : OperationResult<PropertyQuota>.Succeeded(merged);
    }
}
