using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Approvals.Commands;
using LMS.Booking.Application.Approvals.Models;
using LMS.Booking.Application.Approvals.Options;
using LMS.Booking.Application.Approvals.Ports;
using LMS.Booking.Application.Approvals.Services;
using LMS.Booking.Application.Auditing.Ports;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Idempotency.Models;
using LMS.Booking.Application.Idempotency.Options;
using LMS.Booking.Application.Idempotency.Ports;
using LMS.Booking.Application.Idempotency.Services;
using LMS.Booking.Domain.Approvals;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Approvals.Handlers;

// 批量决定：全部成功或整体拒绝（合同建议，Q06 待确认）。全部审核都属于 Booking，因此在一个本地事务内先读取并校验全部，
// 再统一写入；任何校验失败都不产生部分决定。批量重放时按请求中的标识取各审核当前状态（Q09 未决）。
internal sealed class BulkDecideApprovalsHandler(
    ITransactionRunner transactionRunner,
    IApprovalRepository approvals,
    IAuditRecordRepository auditRecords,
    IIdempotencyRecordRepository idempotencyRecords,
    IOptions<IdempotencyOptions> idempotencyOptions,
    IOptions<ApprovalOptions> approvalOptions,
    ApprovalReviewChecks checks,
    TimeProvider timeProvider) : IBulkDecideApprovalsHandler
{
    private const string Operation = "approval.bulk-decide";
    private const string ResourceType = "cross-border-approval";

    public async Task<OperationResult<BulkDecisionResult>> HandleAsync(BulkDecideApprovalsCommand command, CancellationToken cancellationToken)
    {
        if (command.Items.Count > approvalOptions.Value.MaxBulkItems)
        {
            return OperationResult<BulkDecisionResult>.Invalid(
                [new FieldIssue("items", $"Must not contain more than {approvalOptions.Value.MaxBulkItems} items")]);
        }

        var fingerprint = IdempotencyFingerprintFactory.Create(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey), command.ToCanonicalForm());

        return await TryReplayAsync(command, fingerprint, cancellationToken)
            ?? await DecideAsync(command, fingerprint, cancellationToken);
    }

    private async Task<OperationResult<BulkDecisionResult>> DecideAsync(
        BulkDecideApprovalsCommand command,
        IdempotencyFingerprint fingerprint,
        CancellationToken cancellationToken)
    {
        var rejection = await PrecheckAsync(command, cancellationToken);
        if (rejection is not null)
        {
            return rejection;
        }

        try
        {
            return await transactionRunner.ExecuteAsync(
                (context, token) => ApplyAsync(command, fingerprint, context, token), cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            return await TryReplayAsync(command, fingerprint, cancellationToken)
                ?? OperationResult<BulkDecisionResult>.Failed(OperationOutcome.DuplicateConflict);
        }
    }

    // 事务之外的跨域核对（仅批准需要）；事务内会再次读取并校验状态、版本和材料核验。
    private async Task<OperationResult<BulkDecisionResult>?> PrecheckAsync(BulkDecideApprovalsCommand command, CancellationToken cancellationToken)
    {
        var loaded = await approvals.FindVisibleByIdsAsync(command.Items.Select(item => item.ApprovalId).ToList(), command.Context.Scope, cancellationToken);
        var rejection = Verify(command, loaded);
        if (rejection is not null || command.Decision != Domain.Common.ReviewDecision.Approved)
        {
            return rejection;
        }

        var ordered = command.Items.Select(item => loaded.Single(approval => approval.ApprovalId == item.ApprovalId)).ToList();
        var result = await checks.CheckAsync(ordered, index => $"items[{index}].", cancellationToken);

        return result.ToFailure<BulkDecisionResult>();
    }

    // 逐项校验：存在且可见、仍待审核（乐观并发暂时取消，不比较版本）；批准时材料必须全部核验。
    private static OperationResult<BulkDecisionResult>? Verify(BulkDecideApprovalsCommand command, IReadOnlyList<CrossBorderApproval> loaded)
    {
        var byId = loaded.ToDictionary(approval => approval.ApprovalId, StringComparer.Ordinal);
        var missing = command.Items
            .Select((item, index) => (item, index))
            .Where(pair => !byId.ContainsKey(pair.item.ApprovalId))
            .Select(pair => new FieldIssue($"items[{pair.index}].approvalId", "The approval was not found or is not visible"))
            .ToList();
        if (missing.Count > 0)
        {
            return OperationResult<BulkDecisionResult>.Rejected(missing);
        }

        return command.Items.Any(item => byId[item.ApprovalId].Status != ApprovalStatus.Pending)
            ? OperationResult<BulkDecisionResult>.Failed(OperationOutcome.StateConflict)
            : null;
    }

    private async Task<OperationResult<BulkDecisionResult>> ApplyAsync(
        BulkDecideApprovalsCommand command,
        IdempotencyFingerprint fingerprint,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var loaded = await approvals.FindVisibleByIdsAsync(
            command.Items.Select(item => item.ApprovalId).ToList(), command.Context.Scope, context, cancellationToken);
        var rejection = Verify(command, loaded) ?? VerifyDocuments(command, loaded);
        if (rejection is not null)
        {
            return rejection;
        }

        var now = timeProvider.GetUtcNow();
        var outcomes = await WriteAsync(command, loaded, now, context, cancellationToken);
        await idempotencyRecords.InsertAsync(
            new IdempotencyRecord(fingerprint, command.Items[0].ApprovalId, now, now.AddHours(idempotencyOptions.Value.RetentionHours)),
            context,
            cancellationToken);

        return OperationResult<BulkDecisionResult>.Succeeded(new BulkDecisionResult(outcomes, command.Decision));
    }

    private static OperationResult<BulkDecisionResult>? VerifyDocuments(BulkDecideApprovalsCommand command, IReadOnlyList<CrossBorderApproval> loaded)
    {
        if (command.Decision != Domain.Common.ReviewDecision.Approved)
        {
            return null;
        }

        var issues = command.Items
            .Select((item, index) => (approval: loaded.Single(candidate => candidate.ApprovalId == item.ApprovalId), index))
            .Where(pair => !pair.approval.AreAllDocumentsVerified)
            .Select(pair => new FieldIssue($"items[{pair.index}].documents", "All four documents must be verified before approval"))
            .ToList();

        return issues.Count > 0 ? OperationResult<BulkDecisionResult>.Rejected(issues) : null;
    }

    private async Task<IReadOnlyList<ApprovalDecisionOutcome>> WriteAsync(
        BulkDecideApprovalsCommand command,
        IReadOnlyList<CrossBorderApproval> loaded,
        DateTimeOffset now,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var outcomes = new List<ApprovalDecisionOutcome>(command.Items.Count);
        foreach (var item in command.Items)
        {
            var current = loaded.Single(approval => approval.ApprovalId == item.ApprovalId);
            var decided = current.Decide(command.Decision, command.Comments, command.Context.Actor, now);
            var replaced = await approvals.TryReplaceAsync(decided, current.Version, context, cancellationToken);
            if (!replaced)
            {
                throw new InvalidOperationException("An approval changed inside the bulk decision transaction after it was verified.");
            }

            await auditRecords.InsertAsync(CreateAudit(command, decided, now), context, cancellationToken);
            outcomes.Add(new ApprovalDecisionOutcome(decided.ApprovalId, decided.Status, current.Version + 1));
        }

        return outcomes;
    }

    private static Domain.Auditing.AuditRecord CreateAudit(BulkDecideApprovalsCommand command, CrossBorderApproval decided, DateTimeOffset now) =>
        AuditRecordFactory.Succeeded(
            command.Context, Operation, ResourceType, decided.ApprovalId, now,
            $"decision: {LMS.BuildingBlocks.Core.Wire.WireEnum.ToWire(command.Decision)}", decided.PropertyId, null);

    private async Task<OperationResult<BulkDecisionResult>?> TryReplayAsync(
        BulkDecideApprovalsCommand command,
        IdempotencyFingerprint fingerprint,
        CancellationToken cancellationToken)
    {
        var record = await idempotencyRecords.FindActiveAsync(fingerprint, timeProvider.GetUtcNow(), cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (record.Fingerprint.RequestHash != fingerprint.RequestHash)
        {
            return OperationResult<BulkDecisionResult>.Failed(OperationOutcome.IdempotencyKeyReused);
        }

        var current = await approvals.FindByIdsAsync(command.Items.Select(item => item.ApprovalId).ToList(), cancellationToken);
        var outcomes = command.Items
            .Select(item => current.Single(approval => approval.ApprovalId == item.ApprovalId))
            .Select(approval => new ApprovalDecisionOutcome(approval.ApprovalId, approval.Status, approval.Version))
            .ToList();

        return OperationResult<BulkDecisionResult>.Replayed(new BulkDecisionResult(outcomes, command.Decision));
    }
}
