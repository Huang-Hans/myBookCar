using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Ports;

namespace LMS.Dispatch.Application.Workflows;

// 局部更新：读取、版本检查、合并校验、条件替换和审计记录在同一个本地事务内完成。
// 校验失败等非成功结果不写入任何数据。
internal sealed class PatchWorkflow(
    ITransactionRunner transactionRunner,
    IAuditRecordRepository auditRecords,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<TResource>> ExecuteAsync<TResource>(
        PatchSpecification<TResource> specification,
        CancellationToken cancellationToken)
        where TResource : class
    {
        try
        {
            return await transactionRunner.ExecuteAsync(
                (context, token) => PatchAsync(specification, context, token),
                cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            return OperationResult<TResource>.Failed(OperationOutcome.DuplicateConflict);
        }
    }

    private async Task<OperationResult<TResource>> PatchAsync<TResource>(
        PatchSpecification<TResource> specification,
        ITransactionContext context,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var current = await specification.Find(specification.ResourceId, context, cancellationToken);
        var rejection = CheckPrecondition(specification, current);
        if (rejection is not null)
        {
            return rejection;
        }

        var applied = await specification.Apply(current!, context, cancellationToken);
        return applied.IsSuccess ? await SaveAsync(specification, current!, applied.Value!, context, cancellationToken) : applied;
    }

    // 资源不存在（或对调用者不可见）为 NotFound；乐观并发暂时取消，不再比较请求里的期望版本。
    private static OperationResult<TResource>? CheckPrecondition<TResource>(
        PatchSpecification<TResource> specification,
        TResource? current)
        where TResource : class =>
        current is null
            ? OperationResult<TResource>.Failed(OperationOutcome.NotFound)
            : null;

    private async Task<OperationResult<TResource>> SaveAsync<TResource>(
        PatchSpecification<TResource> specification,
        TResource before,
        TResource after,
        ITransactionContext context,
        CancellationToken cancellationToken)
        where TResource : class
    {
        var replaced = await specification.Replace(after, specification.VersionOf(before), context, cancellationToken);
        if (!replaced)
        {
            return OperationResult<TResource>.Failed(OperationOutcome.VersionConflict);
        }

        await auditRecords.InsertAsync(specification.BuildAudit(before, after, timeProvider.GetUtcNow()), context, cancellationToken);
        var saved = await specification.Find(specification.ResourceId, context, cancellationToken);

        return OperationResult<TResource>.Succeeded(saved!);
    }
}
