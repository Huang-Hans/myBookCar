namespace LMS.Booking.Application.Idempotency.Models;

// 与领域数据在同一事务内写入，因此存在即表示原操作已提交。ResultReference 指向原操作的结果资源。
public sealed record IdempotencyRecord(
    IdempotencyFingerprint Fingerprint,
    string ResultReference,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
