using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Idempotency.Models;
using LMS.Booking.Application.Idempotency.Options;
using LMS.Booking.Application.Idempotency.Ports;
using LMS.Booking.Application.Idempotency.Services;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Workflows;

// 不可逆决定：读取、版本检查、前置条件、条件替换、审计和幂等记录在同一个本地事务内提交。
internal sealed class DecisionWorkflow(
    ITransactionRunner transactionRunner,
    PatchWorkflow patchWorkflow,
    IIdempotencyRecordRepository idempotencyRecords,
    IOptions<IdempotencyOptions> idempotencyOptions,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<TResource>> ExecuteAsync<TResource>(
        DecisionSpecification<TResource> specification,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var fingerprint = IdempotencyFingerprintFactory.Create(specification.Scope, specification.RequestCanonicalForm);

        return await TryReplayAsync(specification, fingerprint, cancellationToken)
            ?? await DecideAsync(specification, fingerprint, cancellationToken);
    }

    private async Task<OperationResult<TResource>> DecideAsync<TResource>(
        DecisionSpecification<TResource> specification,
        IdempotencyFingerprint fingerprint,
        CancellationToken cancellationToken)
        where TResource : class
    {
        try
        {
            return await transactionRunner.ExecuteAsync(
                (context, token) => DecideInTransactionAsync(specification, fingerprint, context, token),
                cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            return await TryReplayAsync(specification, fingerprint, cancellationToken)
                ?? OperationResult<TResource>.Failed(OperationOutcome.DuplicateConflict);
        }
    }

    private async Task<OperationResult<TResource>> DecideInTransactionAsync<TResource>(
        DecisionSpecification<TResource> specification,
        IdempotencyFingerprint fingerprint,
        ITransactionContext context,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var result = await patchWorkflow.PatchInTransactionAsync(specification.Patch, context, cancellationToken);
        if (!result.IsSuccess)
        {
            return result;
        }

        var now = timeProvider.GetUtcNow();
        var record = new IdempotencyRecord(
            fingerprint, specification.Patch.ResourceId, now, now.AddHours(idempotencyOptions.Value.RetentionHours));
        await idempotencyRecords.InsertAsync(record, context, cancellationToken);

        return result;
    }

    private async Task<OperationResult<TResource>?> TryReplayAsync<TResource>(
        DecisionSpecification<TResource> specification,
        IdempotencyFingerprint fingerprint,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var record = await idempotencyRecords.FindActiveAsync(fingerprint, timeProvider.GetUtcNow(), cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (record.Fingerprint.RequestHash != fingerprint.RequestHash)
        {
            return OperationResult<TResource>.Failed(OperationOutcome.IdempotencyKeyReused);
        }

        var resource = await specification.Load(record.ResultReference, cancellationToken)
            ?? throw new InvalidOperationException("An idempotency record refers to a resource that does not exist.");

        return OperationResult<TResource>.Replayed(resource);
    }
}
