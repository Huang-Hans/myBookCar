using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Auditing.Ports;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.Configuration.Commands;
using LMS.Platform.Application.Configuration.Ports;

namespace LMS.Platform.Application.Configuration.Handlers;

internal sealed class DeletePropertyHandler(
    ITransactionRunner transactions,
    IPropertyRepository properties,
    IPropertyReferenceDirectory references,
    IAuditRecordRepository audits,
    TimeProvider timeProvider) : IDeletePropertyHandler
{
    public async Task<OperationResult<bool>> HandleAsync(DeletePropertyCommand command, CancellationToken cancellationToken)
    {
        if (!command.Context.Scope.Allows(command.PropertyId))
            return OperationResult<bool>.Failed(OperationOutcome.NotFound);
        var current = await properties.FindAsync(command.PropertyId, cancellationToken);
        if (current is null) return OperationResult<bool>.Failed(OperationOutcome.NotFound);
        if (current.Version != command.ExpectedVersion)
            return OperationResult<bool>.Failed(OperationOutcome.VersionConflict);
        var referenced = await references.HasReferencesAsync(command.PropertyId, cancellationToken);
        if (referenced is null) return OperationResult<bool>.Failed(OperationOutcome.SourceUnavailable);
        if (referenced.Value)
            return OperationResult<bool>.Rejected([new("propertyId", "The property is referenced by existing records and cannot be deleted.")]);
        return await transactions.ExecuteAsync((context, token) => DeleteAsync(command, context, token), cancellationToken);
    }

    private async Task<OperationResult<bool>> DeleteAsync(
        DeletePropertyCommand command, ITransactionContext context, CancellationToken cancellationToken)
    {
        var current = await properties.FindAsync(command.PropertyId, context, cancellationToken);
        if (current is null) return OperationResult<bool>.Failed(OperationOutcome.NotFound);
        if (current.Version != command.ExpectedVersion)
            return OperationResult<bool>.Failed(OperationOutcome.VersionConflict);
        var deleted = current with { IsActive = false, IsDeleted = true, Version = current.Version + 1 };
        if (!await properties.DeleteAsync(deleted, command.Context.Actor, context, cancellationToken))
            return OperationResult<bool>.Failed(OperationOutcome.VersionConflict);
        await audits.InsertAsync(AuditRecordFactory.Succeeded(command.Context, "property.delete", "property",
            current.PropertyId, timeProvider.GetUtcNow(), "logically deleted", current.PropertyId), context, cancellationToken);
        return OperationResult<bool>.Succeeded(true);
    }
}
