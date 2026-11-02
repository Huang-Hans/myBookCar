using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Platform.Domain.CustomerBlacklists;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

internal sealed class CustomerBlacklistDocument
{
    [BsonId]
    public string BlacklistId { get; init; } = string.Empty;

    [BsonElement("customerName")]
    public string CustomerName { get; init; } = string.Empty;

    [BsonElement("memberId")]
    public string MemberId { get; init; } = string.Empty;

    [BsonElement("reason")]
    public string Reason { get; init; } = string.Empty;

    // 业务日期以 YYYY-MM-DD 字符串存储，不转换成 UTC 午夜。
    [BsonElement("effectiveDate")]
    public string EffectiveDate { get; init; } = string.Empty;

    [BsonElement("expiryDate")]
    public string ExpiryDate { get; init; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static CustomerBlacklistDocument FromDomain(CustomerBlacklist blacklist) => new()
    {
        BlacklistId = blacklist.BlacklistId,
        CustomerName = blacklist.CustomerName,
        MemberId = blacklist.MemberId,
        Reason = blacklist.Reason,
        EffectiveDate = StoredValues.FromDate(blacklist.EffectiveDate),
        ExpiryDate = StoredValues.FromDate(blacklist.ExpiryDate),
        IsActive = blacklist.IsActive,
        Version = blacklist.Version
    };

    public CustomerBlacklist ToDomain() => new(
        BlacklistId,
        CustomerName,
        MemberId,
        Reason,
        StoredValues.ToDate(EffectiveDate, "effectiveDate"),
        StoredValues.ToDate(ExpiryDate, "expiryDate"),
        IsActive,
        Version);
}
