namespace LMS.Reporting.Application.Idempotency.Models;

// 三个哈希都是 SHA-256 十六进制串，持久化中不保存键和主体的原值。
public sealed record IdempotencyFingerprint(string ScopeHash, string KeyHash, string RequestHash);
