using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Auditing.Ports;

namespace LMS.Booking.Application.Workflows;

// 本人记录的“读取、版本检查、修改、写入、审计”在同一个本地事务内完成。
// ExpectedVersion 为 0 表示不比较；大于 0 时必须等于当前版本（从未保存过为 0），否则版本冲突。
// 修改后内容与基线相同则不写入，直接返回基线（幂等）。并发的首次写入由键的唯一性裁决，输家按版本冲突处理。
internal sealed class UpsertWorkflow(ITransactionRunner transactionRunner, IAuditRecordRepository auditRecords, TimeProvider timeProvider)
{
    public async Task<OperationResult<TRecord>> ExecuteAsync<TRecord>(UpsertSpecification<TRecord> specification, CancellationToken cancellationToken)
        where TRecord : class
    {
        try
        {
            return await transactionRunner.ExecuteAsync(
                (context, token) => UpsertAsync(specification, context, token),
                cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            return OperationResult<TRecord>.Failed(OperationOutcome.VersionConflict);
        }
    }

    private async Task<OperationResult<TRecord>> UpsertAsync<TRecord>(
        UpsertSpecification<TRecord> specification,
        ITransactionContext context,
        CancellationToken cancellationToken)
        where TRecord : class
    {
        var current = await specification.Find(context, cancellationToken);
        var baseline = current ?? specification.Default;
        if (specification.ExpectedVersion > 0 && specification.ExpectedVersion != specification.VersionOf(baseline))
        {
            return OperationResult<TRecord>.Failed(OperationOutcome.VersionConflict);
        }

        var now = timeProvider.GetUtcNow();
        var modified = specification.Modify(baseline, now);
        if (specification.IsUnchanged(baseline, modified))
        {
            return OperationResult<TRecord>.Succeeded(baseline);
        }

        var saved = await SaveAsync(specification, current, modified, context, cancellationToken);
        if (saved is null)
        {
            return OperationResult<TRecord>.Failed(OperationOutcome.VersionConflict);
        }

        await auditRecords.InsertAsync(specification.BuildAudit(saved, now), context, cancellationToken);

        return OperationResult<TRecord>.Succeeded(saved);
    }

    private static async Task<TRecord?> SaveAsync<TRecord>(
        UpsertSpecification<TRecord> specification,
        TRecord? current,
        TRecord modified,
        ITransactionContext context,
        CancellationToken cancellationToken)
        where TRecord : class
    {
        if (current is null)
        {
            var created = specification.WithVersion(modified, 1);
            await specification.Insert(created, context, cancellationToken);

            return created;
        }

        var version = specification.VersionOf(current);

        return await specification.Replace(modified, version, context, cancellationToken)
            ? specification.WithVersion(modified, version + 1)
            : null;
    }
}
