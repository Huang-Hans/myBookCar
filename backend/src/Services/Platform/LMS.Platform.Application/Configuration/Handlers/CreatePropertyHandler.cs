using System.Text.Json;
using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.Configuration.Commands;
using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Application.Configuration.Ports;
using LMS.Platform.Application.Configuration.Services;
using LMS.Platform.Application.Workflows;

namespace LMS.Platform.Application.Configuration.Handlers;

internal sealed class CreatePropertyHandler(
    CreateWorkflow workflow,
    IPropertyRepository properties,
    IConfigurationListSource<ConfiguredProperty> source) : ICreatePropertyHandler
{
    private const string Operation = "property.create";

    public async Task<OperationResult<ConfiguredProperty>> HandleAsync(CreatePropertyCommand command, CancellationToken cancellationToken)
    {
        var rejection = Validate(command);
        if (rejection is not null) return rejection;
        var current = await source.LoadAsync(cancellationToken);
        return current.IsAvailable
            ? await workflow.ExecuteAsync(Specification(command), cancellationToken)
            : OperationResult<ConfiguredProperty>.Failed(OperationOutcome.SourceUnavailable);
    }

    private static OperationResult<ConfiguredProperty>? Validate(CreatePropertyCommand command)
    {
        if (!command.Context.Scope.IsAllProperties)
            return OperationResult<ConfiguredProperty>.Failed(OperationOutcome.Forbidden);
        var issues = PropertyRules.Validate(command.Property);
        return issues.Count > 0 ? OperationResult<ConfiguredProperty>.Invalid(issues) : null;
    }

    private CreateSpecification<ConfiguredProperty> Specification(CreatePropertyCommand command) => new(
        IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
        JsonSerializer.Serialize(command.Property),
        (_, _) => Task.FromResult(command.Property),
        property => property.PropertyId,
        (property, context, token) => properties.InsertAsync(property, command.Context.Actor, context, token),
        properties.FindAsync,
        (property, now) => AuditRecordFactory.Created(
            command.Context, Operation, "property", property.PropertyId, now, property.PropertyId));
}
