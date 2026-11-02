using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Auditing.Ports;
using LMS.Platform.Application.ConfigurationReleases.Models;
using LMS.Platform.Application.ConfigurationReleases.Ports;
using LMS.Platform.Application.ConfigurationReleases.Requests;
using LMS.Platform.Application.Idempotency.Models;
using LMS.Platform.Application.Idempotency.Options;
using LMS.Platform.Application.Idempotency.Ports;
using LMS.Platform.Application.Idempotency.Services;
using LMS.Platform.Domain.Auditing;
using LMS.Platform.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.ConfigurationReleases.Handlers;

// 发布配置元数据：生效记录切换、发布记录、审计记录和幂等记录在同一个本地事务内提交。
// 事务内先做版本检查和条件切换，通过后才写其余记录，因此冲突时不会留下部分写入。
internal sealed class PublishConfigurationReleaseHandler(
    ITransactionRunner transactionRunner,
    IConfigurationReleaseRepository releases,
    IActiveConfigurationRepository activeConfigurations,
    IAuditRecordRepository auditRecords,
    IIdempotencyRecordRepository idempotencyRecords,
    IOptions<IdempotencyOptions> idempotencyOptions,
    TimeProvider timeProvider) : IPublishConfigurationReleaseHandler
{
    private const string ServiceName = "platform";
    private const string OperationName = "configuration-release.publish";
    private const string ResourceType = "configuration-release";

    public async Task<PublishConfigurationReleaseResult> HandleAsync(
        PublishConfigurationReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var scope = new IdempotencyScope(ServiceName, OperationName, request.Actor, request.IdempotencyKey);
        var fingerprint = IdempotencyFingerprintFactory.Create(scope, request.ToCanonicalForm());

        var replayed = await TryReplayAsync(fingerprint, cancellationToken);
        return replayed ?? await PublishInTransactionAsync(request, fingerprint, cancellationToken);
    }

    // 版本冲突有两种来源：他人先推进了生效版本，或同一请求（相同幂等键）已在本次首次重放检查之后提交。
    // 因此冲突后再查一次幂等记录，存在则按重放返回，否则才是真正的版本冲突。
    private async Task<PublishConfigurationReleaseResult> PublishInTransactionAsync(
        PublishConfigurationReleaseRequest request,
        IdempotencyFingerprint fingerprint,
        CancellationToken cancellationToken)
    {
        var result = await TryPublishAsync(request, fingerprint, cancellationToken);
        if (result.Outcome != PublishOutcome.VersionConflict)
        {
            return result;
        }

        return await TryReplayAsync(fingerprint, cancellationToken) ?? result;
    }

    // 并发竞争时唯一约束会让后到的事务失败，这同样按版本冲突处理。
    private async Task<PublishConfigurationReleaseResult> TryPublishAsync(
        PublishConfigurationReleaseRequest request,
        IdempotencyFingerprint fingerprint,
        CancellationToken cancellationToken)
    {
        try
        {
            return await transactionRunner.ExecuteAsync(
                (context, token) => PublishAsync(request, fingerprint, context, token),
                cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            return PublishConfigurationReleaseResult.VersionConflict();
        }
    }

    private async Task<PublishConfigurationReleaseResult> PublishAsync(
        PublishConfigurationReleaseRequest request,
        IdempotencyFingerprint fingerprint,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var active = await activeConfigurations.FindAsync(request.Environment, context, cancellationToken);
        if (active?.Version != request.ExpectedActiveVersion)
        {
            return PublishConfigurationReleaseResult.VersionConflict();
        }

        var now = timeProvider.GetUtcNow();
        var release = ConfigurationRelease.Create(
            request.Environment, NextSequence(active), request.Digest, request.ApprovedBy, request.EffectiveAt, now);
        if (!await activeConfigurations.AdvanceAsync(active, release, context, cancellationToken))
        {
            return PublishConfigurationReleaseResult.VersionConflict();
        }

        await WriteRecordsAsync(request, fingerprint, release, context, cancellationToken);
        return PublishConfigurationReleaseResult.Created(release);
    }

    private async Task WriteRecordsAsync(
        PublishConfigurationReleaseRequest request,
        IdempotencyFingerprint fingerprint,
        ConfigurationRelease release,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        await releases.InsertAsync(release, context, cancellationToken);
        await auditRecords.InsertAsync(CreateAuditRecord(request, release), context, cancellationToken);
        await idempotencyRecords.InsertAsync(CreateIdempotencyRecord(fingerprint, release), context, cancellationToken);
    }

    private async Task<PublishConfigurationReleaseResult?> TryReplayAsync(
        IdempotencyFingerprint fingerprint,
        CancellationToken cancellationToken)
    {
        var record = await idempotencyRecords.FindActiveAsync(fingerprint, timeProvider.GetUtcNow(), cancellationToken);

        return record is null ? null : await ReplayAsync(record, fingerprint, cancellationToken);
    }

    private async Task<PublishConfigurationReleaseResult> ReplayAsync(
        IdempotencyRecord record,
        IdempotencyFingerprint fingerprint,
        CancellationToken cancellationToken)
    {
        if (record.Fingerprint.RequestHash != fingerprint.RequestHash)
        {
            return PublishConfigurationReleaseResult.IdempotencyKeyReused();
        }

        var release = await releases.FindAsync(record.ResultReference, cancellationToken)
            ?? throw new InvalidOperationException("An idempotency record refers to a configuration release that does not exist.");

        return PublishConfigurationReleaseResult.Replayed(release);
    }

    private static long NextSequence(ActiveConfiguration? active) => (active?.Sequence ?? 0) + 1;

    private static AuditRecord CreateAuditRecord(PublishConfigurationReleaseRequest request, ConfigurationRelease release) =>
        new(
            AuditRecord.NewId(),
            request.Actor,
            OperationName,
            ResourceType,
            release.ReleaseId,
            release.CreatedAt,
            AuditOutcome.Succeeded,
            request.TraceId,
            ServiceName,
            $"environment={release.Environment};sequence={release.Sequence};digest={release.Digest}",
            null);

    private IdempotencyRecord CreateIdempotencyRecord(IdempotencyFingerprint fingerprint, ConfigurationRelease release) =>
        new(
            fingerprint,
            release.ReleaseId,
            release.CreatedAt,
            release.CreatedAt.AddHours(idempotencyOptions.Value.RetentionHours));
}
