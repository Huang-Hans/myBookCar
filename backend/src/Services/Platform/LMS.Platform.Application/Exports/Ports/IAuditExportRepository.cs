using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Exports;

namespace LMS.Platform.Application.Exports.Ports;

public interface IAuditExportRepository
{
    Task<AuditExport?> FindAsync(string exportId, CancellationToken cancellationToken);

    Task InsertAsync(AuditExport export, ITransactionContext context, CancellationToken cancellationToken);

    // 原子领取最早的待执行作业：QUEUED，或租约已过期的 RUNNING；领取后置为 RUNNING 并写入新的租约。
    Task<AuditExport?> ClaimNextAsync(DateTimeOffset now, DateTimeOffset leaseExpiresAt, CancellationToken cancellationToken);

    // 仅当作业仍是 RUNNING 时置为 SUCCEEDED 并登记结果文件；与结果文件元数据在同一事务提交。
    Task<bool> CompleteAsync(
        string exportId,
        string resultFileId,
        DateTimeOffset expiresAt,
        ITransactionContext context,
        CancellationToken cancellationToken);

    Task<bool> FailAsync(string exportId, string failureMessage, CancellationToken cancellationToken);
}
