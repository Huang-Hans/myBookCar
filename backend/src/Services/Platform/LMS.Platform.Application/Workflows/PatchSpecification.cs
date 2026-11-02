using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Auditing;

namespace LMS.Platform.Application.Workflows;

// 一次“按期望版本局部更新”所需的差异点；通用流程由 PatchWorkflow 执行。
// Find 必须带授权范围，不可见的资源按不存在处理；Apply 负责合并并校验，失败时返回对应的失败结果。
internal sealed record PatchSpecification<TResource>(
    string ResourceId,
    long ExpectedVersion,
    Func<string, ITransactionContext, CancellationToken, Task<TResource?>> Find,
    Func<TResource, long> VersionOf,
    Func<TResource, ITransactionContext, CancellationToken, Task<OperationResult<TResource>>> Apply,
    Func<TResource, long, ITransactionContext, CancellationToken, Task<bool>> Replace,
    Func<TResource, TResource, DateTimeOffset, AuditRecord> BuildAudit);
