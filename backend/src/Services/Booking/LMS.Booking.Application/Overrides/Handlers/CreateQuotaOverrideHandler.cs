using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Overrides.Commands;
using LMS.Booking.Application.Overrides.Ports;
using LMS.Booking.Application.Quotas.Ports;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Overrides;

namespace LMS.Booking.Application.Overrides.Handlers;

// 申请额外每日权益：只登记待审批申请，不修改任何限额。同物业、同客人只允许一条待审批申请（由唯一索引保证）。
internal sealed class CreateQuotaOverrideHandler(
    CreateWorkflow workflow,
    IQuotaOverrideRepository overrides,
    IPropertyQuotaRepository quotas,
    TimeProvider timeProvider) : ICreateQuotaOverrideHandler
{
    private const string Operation = "quota-override.create";
    private const string ResourceType = "quota-override";

    public async Task<OperationResult<QuotaOverride>> HandleAsync(CreateQuotaOverrideCommand command, CancellationToken cancellationToken)
    {
        var rejection = await CheckAsync(command, cancellationToken);

        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
    }

    private async Task<OperationResult<QuotaOverride>?> CheckAsync(CreateQuotaOverrideCommand command, CancellationToken cancellationToken)
    {
        if (!command.Context.Scope.Allows(command.PropertyId))
        {
            return OperationResult<QuotaOverride>.Failed(OperationOutcome.Forbidden);
        }

        var isInRange = command.AdditionalDailyEntitlements is >= QuotaOverride.MinAdditionalEntitlements and <= QuotaOverride.MaxAdditionalEntitlements;
        if (!isInRange)
        {
            return OperationResult<QuotaOverride>.Invalid(
                [new FieldIssue("additionalDailyEntitlements", $"Must be between {QuotaOverride.MinAdditionalEntitlements} and {QuotaOverride.MaxAdditionalEntitlements}")]);
        }

        var hasQuota = await quotas.FindByIdAsync(command.PropertyId, cancellationToken) is not null;

        return hasQuota ? null : OperationResult<QuotaOverride>.Invalid([new FieldIssue("propertyId", "No quota is configured for the property")]);
    }

    private CreateSpecification<QuotaOverride> BuildSpecification(CreateQuotaOverrideCommand command) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(QuotaOverride.Create(
                command.PropertyId, command.GuestId, command.AdditionalDailyEntitlements, command.Justification,
                command.Context.Actor, timeProvider.GetUtcNow())),
            quotaOverride => quotaOverride.OverrideId,
            (quotaOverride, context, token) => overrides.InsertAsync(quotaOverride, context, token),
            (id, token) => overrides.FindByIdAsync(id, token),
            (quotaOverride, now) => AuditRecordFactory.Created(
                command.Context, Operation, ResourceType, quotaOverride.OverrideId, now, quotaOverride.PropertyId));
}
