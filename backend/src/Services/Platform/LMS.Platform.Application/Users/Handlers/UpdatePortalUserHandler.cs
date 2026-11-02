using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.Configuration.Catalogs;
using LMS.Platform.Application.Users.Commands;
using LMS.Platform.Application.Users.Ports;
using LMS.Platform.Application.Users.Services;
using LMS.Platform.Application.Workflows;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Users.Handlers;

internal sealed class UpdatePortalUserHandler(
    PatchWorkflow workflow,
    IPortalUserRepository users,
    IPropertyCatalog properties) : IUpdatePortalUserHandler
{
    private const string Operation = "portal-user.update";
    private const string ResourceType = "portal-user";

    public async Task<OperationResult<PortalUser>> HandleAsync(UpdatePortalUserCommand command, CancellationToken cancellationToken)
    {
        var changedProperty = command.Changes.PropertyId;
        var rejection = changedProperty.IsSet
            ? await PropertyReferenceCheck.ResolveAsync<PortalUser>(properties, changedProperty.Value, cancellationToken)
            : null;

        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
    }

    private PatchSpecification<PortalUser> BuildSpecification(UpdatePortalUserCommand command) =>
        new(
            command.UserId,
            command.ExpectedVersion,
            (id, context, token) => users.FindVisibleAsync(id, command.Context.Scope, context, token),
            user => user.Version,
            (user, _, _) => Task.FromResult(PortalUserChecks.Evaluate(user.Apply(command.Changes), command.Context.Scope)),
            (updated, expected, context, token) => users.TryReplaceAsync(updated, expected, context, token),
            (_, after, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, after.UserId, now, command.Changes.ChangedFields(), after.PropertyId));
}
