using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Auditing;

namespace LMS.Booking.Application.Workflows;

// 一次“带期望版本的本人记录保存”所需的全部差异点；通用流程由 UpsertWorkflow 执行。
// 记录键就是本人标识（或本人标识加用途），因此不存在跨人覆盖；从未保存过的记录用 Default（版本 0）作为修改基线。
internal sealed record UpsertSpecification<TRecord>(
    long ExpectedVersion,
    TRecord Default,
    Func<ITransactionContext, CancellationToken, Task<TRecord?>> Find,
    Func<TRecord, long> VersionOf,
    Func<TRecord, long, TRecord> WithVersion,
    Func<TRecord, DateTimeOffset, TRecord> Modify,
    Func<TRecord, TRecord, bool> IsUnchanged,
    Func<TRecord, ITransactionContext, CancellationToken, Task> Insert,
    Func<TRecord, long, ITransactionContext, CancellationToken, Task<bool>> Replace,
    Func<TRecord, DateTimeOffset, AuditRecord> BuildAudit);
