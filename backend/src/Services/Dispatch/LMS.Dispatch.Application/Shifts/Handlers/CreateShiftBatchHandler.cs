using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Idempotency.Models;
using LMS.Dispatch.Application.Idempotency.Services;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Services;
using LMS.Dispatch.Domain.Shifts.Batches;

namespace LMS.Dispatch.Application.Shifts.Handlers;

internal sealed class CreateShiftBatchHandler(
    ITransactionRunner transactions, ShiftBatchPropertyCheck properties, ShiftBatchPlanner planner,
    ShiftBatchReplayProbe replays, ShiftBatchWriter writer) : ICreateShiftBatchHandler
{
    public async Task<OperationResult<ShiftBatch>> HandleAsync(CreateShiftBatchCommand command, CancellationToken cancellationToken)
    {
        var fingerprint = IdempotencyFingerprintFactory.Create(
            IdempotencyScopes.For(command.Context, "shift.batch-create", command.IdempotencyKey), command.ToCanonicalForm());
        var replay = await replays.FindAsync(command, fingerprint, cancellationToken);
        if (replay is not null) return replay;
        var failure = await properties.CheckAsync<ShiftBatch>(command.Rules, command.Context.Scope, cancellationToken);
        return failure ?? await CreateAsync(command, fingerprint, cancellationToken);
    }

    private async Task<OperationResult<ShiftBatch>> CreateAsync(CreateShiftBatchCommand command,
        IdempotencyFingerprint fingerprint, CancellationToken cancellationToken)
    {
        try
        {
            var result = await transactions.ExecuteAsync((context, token) => CommitAsync(command, fingerprint, context, token), cancellationToken);
            if (result.Outcome != OperationOutcome.StateConflict) return result;
            // A transaction retry can observe the shifts committed by an identical concurrent request.
            return await replays.FindAsync(command, fingerprint, cancellationToken) ?? result;
        }
        catch (ShiftSchemaUpgradeRequiredException)
        {
            return await replays.FindAsync(command, fingerprint, cancellationToken)
                ?? new(OperationOutcome.SourceUnavailable, null,
                    [new("scheduling", "Scheduling rules require a database update. Contact an administrator before retrying.")]);
        }
        catch (DuplicateRecordException)
        {
            return await replays.FindAsync(command, fingerprint, cancellationToken)
                ?? OperationResult<ShiftBatch>.Failed(OperationOutcome.StateConflict);
        }
    }

    private async Task<OperationResult<ShiftBatch>> CommitAsync(CreateShiftBatchCommand command,
        IdempotencyFingerprint fingerprint, ITransactionContext context, CancellationToken cancellationToken)
    {
        var planned = await planner.PlanAsync(command.Rules, command.Context.Scope, context, cancellationToken);
        if (!planned.IsSuccess) return OperationResult<ShiftBatch>.FailureOf(planned);
        var preview = planned.Value!;
        var rejection = ValidateConfirmation(command, preview);
        if (rejection is not null) return rejection;
        return OperationResult<ShiftBatch>.Created(await writer.WriteAsync(command, preview, fingerprint, context, cancellationToken));
    }

    private static OperationResult<ShiftBatch>? ValidateConfirmation(CreateShiftBatchCommand command, ShiftBatchPreview preview)
    {
        if (preview.PreviewHash != command.PreviewHash)
            return OperationResult<ShiftBatch>.Failed(OperationOutcome.StateConflict);
        return preview.CreateCount > 0 ? null
            : OperationResult<ShiftBatch>.Invalid([new("driverIds", "There are no new shifts to create")]);
    }
}
