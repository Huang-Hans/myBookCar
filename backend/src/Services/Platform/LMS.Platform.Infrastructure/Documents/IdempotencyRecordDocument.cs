using LMS.Platform.Application.Idempotency.Models;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

internal sealed class IdempotencyRecordDocument
{
    // 由作用域哈希和键哈希组合而成，_id 的唯一性即“同作用域同键只能有一条记录”，并发写入由它仲裁。
    [BsonId]
    public string Id { get; init; } = string.Empty;

    [BsonElement("scopeHash")]
    public string ScopeHash { get; init; } = string.Empty;

    [BsonElement("keyHash")]
    public string KeyHash { get; init; } = string.Empty;

    [BsonElement("requestHash")]
    public string RequestHash { get; init; } = string.Empty;

    [BsonElement("resultReference")]
    public string ResultReference { get; init; } = string.Empty;

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; }

    // TTL 索引据此异步清理；是否过期仍以查询时的显式比较为准。
    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; init; }

    public static string ComposeId(IdempotencyFingerprint fingerprint) => $"{fingerprint.ScopeHash}:{fingerprint.KeyHash}";

    public static IdempotencyRecordDocument FromDomain(IdempotencyRecord record) => new()
    {
        Id = ComposeId(record.Fingerprint),
        ScopeHash = record.Fingerprint.ScopeHash,
        KeyHash = record.Fingerprint.KeyHash,
        RequestHash = record.Fingerprint.RequestHash,
        ResultReference = record.ResultReference,
        CreatedAt = UtcDateTimes.ToStored(record.CreatedAt),
        ExpiresAt = UtcDateTimes.ToStored(record.ExpiresAt)
    };

    public IdempotencyRecord ToDomain() => new(
        new IdempotencyFingerprint(ScopeHash, KeyHash, RequestHash),
        ResultReference,
        UtcDateTimes.FromStored(CreatedAt),
        UtcDateTimes.FromStored(ExpiresAt));
}
