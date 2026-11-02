using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.CustomerBlacklists.Commands;
using LMS.Platform.Application.CustomerBlacklists.Ports;
using LMS.Platform.Application.Workflows;
using LMS.Platform.Domain.CustomerBlacklists;

namespace LMS.Platform.Application.CustomerBlacklists.Handlers;

internal sealed class CreateCustomerBlacklistHandler(
    CreateWorkflow workflow,
    ICustomerBlacklistRepository blacklists) : ICreateCustomerBlacklistHandler
{
    private const string Operation = "customer-blacklist.create";
    private const string ResourceType = "customer-blacklist";

    public async Task<OperationResult<CustomerBlacklist>> HandleAsync(
        CreateCustomerBlacklistCommand command,
        CancellationToken cancellationToken)
    {
        var candidate = CustomerBlacklist.Create(
            command.CustomerName, command.MemberId, command.Reason, command.EffectiveDate, command.ExpiryDate, command.IsActive);
        var issues = CustomerBlacklistRules.Validate(candidate);
        if (issues.Count > 0)
        {
            return OperationResult<CustomerBlacklist>.Invalid(issues);
        }

        return await workflow.ExecuteAsync(BuildSpecification(command, candidate), cancellationToken);
    }

    private CreateSpecification<CustomerBlacklist> BuildSpecification(CreateCustomerBlacklistCommand command, CustomerBlacklist candidate) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(candidate),
            blacklist => blacklist.BlacklistId,
            (blacklist, context, token) => blacklists.InsertAsync(blacklist, context, token),
            (id, token) => blacklists.FindByIdAsync(id, token),
            (blacklist, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, blacklist.BlacklistId, now, null));
}
