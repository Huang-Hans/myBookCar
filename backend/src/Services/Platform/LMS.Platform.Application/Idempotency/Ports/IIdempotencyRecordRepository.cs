using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Idempotency.Models;

namespace LMS.Platform.Application.Idempotency.Ports;

public interface IIdempotencyRecordRepository
{
    // 只返回在 now 时刻仍有效的记录，逻辑过期不依赖 TTL 清理。
    Task<IdempotencyRecord?> FindActiveAsync(IdempotencyFingerprint fingerprint, DateTimeOffset now, CancellationToken cancellationToken);

    Task InsertAsync(IdempotencyRecord record, ITransactionContext context, CancellationToken cancellationToken);
}
