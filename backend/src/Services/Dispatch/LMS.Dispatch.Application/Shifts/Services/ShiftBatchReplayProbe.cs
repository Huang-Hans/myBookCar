using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Idempotency.Models;
using LMS.Dispatch.Application.Idempotency.Ports;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Domain.Shifts.Batches;

namespace LMS.Dispatch.Application.Shifts.Services;

internal sealed class ShiftBatchReplayProbe(
    IIdempotencyRecordRepository records, IShiftBatchRepository batches, IDriverRepository drivers, TimeProvider timeProvider)
{
    public async Task<OperationResult<ShiftBatch>?> FindAsync(CreateShiftBatchCommand command,
        IdempotencyFingerprint fingerprint, CancellationToken cancellationToken)
    {
        var record = await records.FindActiveAsync(fingerprint, timeProvider.GetUtcNow(), cancellationToken);
        if (record is null) return null;
        if (record.Fingerprint.RequestHash != fingerprint.RequestHash)
            return OperationResult<ShiftBatch>.Failed(OperationOutcome.IdempotencyKeyReused);
        return await LoadAsync(command, record.ResultReference, cancellationToken);
    }

    private async Task<OperationResult<ShiftBatch>> LoadAsync(CreateShiftBatchCommand command, string batchId, CancellationToken cancellationToken)
    {
        var batch = await batches.FindAsync(batchId, cancellationToken)
            ?? throw new InvalidOperationException("An idempotency record refers to a missing shift batch.");
        if (batch.Actor != command.Context.Actor)
            return OperationResult<ShiftBatch>.Failed(OperationOutcome.Forbidden);
        var current = await drivers.FindByIdsAsync(batch.Items.Select(item => item.DriverId).Distinct().ToList(), cancellationToken);
        var byId = current.ToDictionary(driver => driver.DriverId, StringComparer.Ordinal);
        var safeItems = batch.Items.Select(item => Redact(item, command, byId)).ToList();
        return OperationResult<ShiftBatch>.Replayed(batch with { Items = safeItems });
    }

    private static ShiftBatchItem Redact(ShiftBatchItem item, CreateShiftBatchCommand command,
        IReadOnlyDictionary<string, Driver> drivers) =>
        drivers.TryGetValue(item.DriverId, out var driver) && command.Context.Scope.Allows(driver.PropertyId)
            ? item with { DriverName = driver.Name, PropertyId = driver.PropertyId }
            : item with { DriverName = string.Empty, Period = null, DutyStatus = null, Role = null, ShiftId = null, PropertyId = null };
}
