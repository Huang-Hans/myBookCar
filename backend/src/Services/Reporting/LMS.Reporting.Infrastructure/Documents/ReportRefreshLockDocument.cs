using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Reporting.Infrastructure.Documents;

// report_refresh_locks：_id 为锁键（例如 day:20261019），Holder 为持有实例，ExpiresAt 之后可被其他实例接管，TTL 索引负责清理。
internal sealed class ReportRefreshLockDocument
{
    [BsonId]
    public string Key { get; init; } = string.Empty;

    [BsonElement("holder")]
    public string Holder { get; init; } = string.Empty;

    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; init; }
}
