using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Idempotency.Models;

namespace LMS.Dispatch.Application.Idempotency.Ports;

public interface IIdempotencyRecordRepository
{
    // 只返回在 now 时刻仍有效的记录，逻辑过期不依赖 TTL 清理。
    Task<IdempotencyRecord?> FindActiveAsync(IdempotencyFingerprint fingerprint, DateTimeOffset now, CancellationToken cancellationToken);

    Task InsertAsync(IdempotencyRecord record, ITransactionContext context, CancellationToken cancellationToken);
}
