using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.CustomerBlacklists.Commands;
using LMS.Platform.Application.CustomerBlacklists.Ports;
using LMS.Platform.Application.Workflows;
using LMS.Platform.Domain.CustomerBlacklists;

namespace LMS.Platform.Application.CustomerBlacklists.Handlers;

internal sealed class UpdateCustomerBlacklistHandler(
    PatchWorkflow workflow,
    ICustomerBlacklistRepository blacklists) : IUpdateCustomerBlacklistHandler
{
    private const string Operation = "customer-blacklist.update";
    private const string ResourceType = "customer-blacklist";

    public Task<OperationResult<CustomerBlacklist>> HandleAsync(UpdateCustomerBlacklistCommand command, CancellationToken cancellationToken) =>
        workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);

    private PatchSpecification<CustomerBlacklist> BuildSpecification(UpdateCustomerBlacklistCommand command) =>
        new(
            command.BlacklistId,
            command.ExpectedVersion,
            (id, context, token) => blacklists.FindAsync(id, context, token),
            blacklist => blacklist.Version,
            (blacklist, _, _) => Task.FromResult(Merge(blacklist, command)),
            (updated, expected, context, token) => blacklists.TryReplaceAsync(updated, expected, context, token),
            (_, after, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, after.BlacklistId, now, command.Changes.ChangedFields(), null));

    private static OperationResult<CustomerBlacklist> Merge(CustomerBlacklist current, UpdateCustomerBlacklistCommand command)
    {
        var merged = current.Apply(command.Changes);
        var issues = CustomerBlacklistRules.Validate(merged);

        return issues.Count > 0 ? OperationResult<CustomerBlacklist>.Invalid(issues) : OperationResult<CustomerBlacklist>.Succeeded(merged);
    }
}
