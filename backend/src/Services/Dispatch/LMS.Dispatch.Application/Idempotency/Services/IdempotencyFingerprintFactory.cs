using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LMS.Dispatch.Application.Idempotency.Models;

namespace LMS.Dispatch.Application.Idempotency.Services;

public static class IdempotencyFingerprintFactory
{
    // requestCanonicalForm 必须只包含语义输入，排除 traceId 等传输字段。
    public static IdempotencyFingerprint Create(IdempotencyScope scope, string requestCanonicalForm) =>
        new(
            ComputeHash(scope.ServiceName, scope.Operation, scope.Subject),
            ComputeHash(scope.Key),
            ComputeHash(requestCanonicalForm));

    // 用 JSON 数组做规范化，避免字段值中出现分隔符造成歧义。
    private static string ComputeHash(params string[] parts)
    {
        var canonicalText = JsonSerializer.Serialize(parts);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText))).ToLowerInvariant();
    }
}
