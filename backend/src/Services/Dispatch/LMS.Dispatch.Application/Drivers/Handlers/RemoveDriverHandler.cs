using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Ports;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Drivers.Commands;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Handlers;

internal sealed class RemoveDriverHandler(
    ITransactionRunner transactions,
    IDriverRepository drivers,
    IAuditRecordRepository auditRecords,
    TimeProvider timeProvider) : IRemoveDriverHandler
{
    public Task<OperationResult<bool>> HandleAsync(RemoveDriverCommand command, CancellationToken cancellationToken) =>
        transactions.ExecuteAsync((context, token) => RemoveAsync(command, context, token), cancellationToken);

    private async Task<OperationResult<bool>> RemoveAsync(RemoveDriverCommand command,
        ITransactionContext context, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindVisibleAsync(command.DriverId, command.Context.Scope, context, cancellationToken);
        if (driver is null) return OperationResult<bool>.Failed(OperationOutcome.NotFound);
        if (driver.Availability == DriverAvailability.OnTrip)
            return new(OperationOutcome.StateConflict, false, [new("driverId", "A driver on a trip cannot be deleted.")]);
        return await SaveRemovalAsync(command, driver, context, cancellationToken);
    }

    private async Task<OperationResult<bool>> SaveRemovalAsync(RemoveDriverCommand command, Driver driver,
        ITransactionContext context, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var removed = driver with { DeletedAt = now, Status = DriverStatus.Inactive };
        if (!await drivers.TryReplaceAsync(removed, driver.Version, context, cancellationToken))
            return OperationResult<bool>.Failed(OperationOutcome.VersionConflict);
        await auditRecords.InsertAsync(AuditRecordFactory.Succeeded(command.Context, "driver.remove", "driver",
            driver.DriverId, now, "removed", driver.PropertyId), context, cancellationToken);
        return OperationResult<bool>.Succeeded(true);
    }
}
