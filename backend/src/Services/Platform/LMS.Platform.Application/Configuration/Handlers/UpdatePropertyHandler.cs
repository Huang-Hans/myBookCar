using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.Configuration.Commands;
using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Application.Configuration.Ports;
using LMS.Platform.Application.Configuration.Services;
using LMS.Platform.Application.Workflows;

namespace LMS.Platform.Application.Configuration.Handlers;

internal sealed class UpdatePropertyHandler(
    PatchWorkflow workflow,
    IPropertyRepository properties,
    IConfigurationListSource<ConfiguredProperty> source) : IUpdatePropertyHandler
{
    private const string Operation = "property.update";

    public async Task<OperationResult<ConfiguredProperty>> HandleAsync(UpdatePropertyCommand command, CancellationToken cancellationToken)
    {
        if (!command.Context.Scope.Allows(command.PropertyId))
            return OperationResult<ConfiguredProperty>.Failed(OperationOutcome.NotFound);
        var current = await source.LoadAsync(cancellationToken);
        return current.IsAvailable
            ? await workflow.ExecuteAsync(Specification(command), cancellationToken)
            : OperationResult<ConfiguredProperty>.Failed(OperationOutcome.SourceUnavailable);
    }

    private PatchSpecification<ConfiguredProperty> Specification(UpdatePropertyCommand command) => new(
        command.PropertyId, command.ExpectedVersion, properties.FindAsync, property => property.Version,
        (property, _, _) => Task.FromResult(Apply(property, command.Changes)),
        (property, _, context, token) => properties.ReplaceAsync(property, command.Context.Actor, context, token),
        (_, property, now) => AuditRecordFactory.Updated(
            command.Context, Operation, "property", property.PropertyId, now, command.Changes.ChangedFields(), property.PropertyId));

    private static OperationResult<ConfiguredProperty> Apply(ConfiguredProperty current, PropertyChanges changes)
    {
        var updated = changes.Apply(current);
        var issues = PropertyRules.Validate(updated);
        return issues.Count > 0
            ? OperationResult<ConfiguredProperty>.Invalid(issues)
            : OperationResult<ConfiguredProperty>.Succeeded(updated);
    }
}
