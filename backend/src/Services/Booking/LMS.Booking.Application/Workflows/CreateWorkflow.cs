using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Auditing.Ports;
using LMS.Booking.Application.Idempotency.Models;
using LMS.Booking.Application.Idempotency.Options;
using LMS.Booking.Application.Idempotency.Ports;
using LMS.Booking.Application.Idempotency.Services;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Workflows;

// 创建资源：资源、审计记录和幂等记录在同一个本地事务内提交。
// 相同幂等键且内容一致时返回已创建的资源（Q09 未定前返回资源当前状态，而非原响应副本）。
internal sealed class CreateWorkflow(
    ITransactionRunner transactionRunner,
    IAuditRecordRepository auditRecords,
    IIdempotencyRecordRepository idempotencyRecords,
    IOptions<IdempotencyOptions> idempotencyOptions,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<TResource>> ExecuteAsync<TResource>(
        CreateSpecification<TResource> specification,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var fingerprint = IdempotencyFingerprintFactory.Create(specification.Scope, specification.RequestCanonicalForm);

        return await TryReplayAsync(specification, fingerprint, cancellationToken)
            ?? await CreateAsync(specification, fingerprint, cancellationToken);
    }

    // 唯一约束冲突有两种来源：并发的相同幂等请求先提交，或业务唯一值重复。先按重放处理，再判定为重复。
    private async Task<OperationResult<TResource>> CreateAsync<TResource>(
        CreateSpecification<TResource> specification,
        IdempotencyFingerprint fingerprint,
        CancellationToken cancellationToken)
        where TResource : class
    {
        try
        {
            var resource = await transactionRunner.ExecuteAsync(
                (context, token) => InsertAllAsync(specification, fingerprint, context, token),
                cancellationToken);

            return OperationResult<TResource>.Created(resource);
        }
        catch (DuplicateRecordException)
        {
            return await TryReplayAsync(specification, fingerprint, cancellationToken)
                ?? OperationResult<TResource>.Failed(OperationOutcome.DuplicateConflict);
        }
    }

    private async Task<TResource> InsertAllAsync<TResource>(
        CreateSpecification<TResource> specification,
        IdempotencyFingerprint fingerprint,
        ITransactionContext context,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var resource = await specification.Build(context, cancellationToken);
        var now = timeProvider.GetUtcNow();

        await specification.Insert(resource, context, cancellationToken);
        await auditRecords.InsertAsync(specification.BuildAudit(resource, now), context, cancellationToken);
        await idempotencyRecords.InsertAsync(
            new IdempotencyRecord(fingerprint, specification.IdOf(resource), now, now.AddHours(idempotencyOptions.Value.RetentionHours)),
            context,
            cancellationToken);

        return resource;
    }

    private async Task<OperationResult<TResource>?> TryReplayAsync<TResource>(
        CreateSpecification<TResource> specification,
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
