using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Platform.Domain.Authentication;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

// app_refresh_tokens：司机/客户共用的刷新凭据记录。不保存明文凭据，也不是账号表。
internal sealed class AppRefreshTokenDocument
{
    [BsonId]
    public string TokenId { get; init; } = string.Empty;

    [BsonElement("tokenHash")]
    public string TokenHash { get; init; } = string.Empty;

    [BsonElement("principalType")]
    public string PrincipalType { get; init; } = string.Empty;

    [BsonElement("entityId")]
    public string EntityId { get; init; } = string.Empty;

    [BsonElement("familyId")]
    public string FamilyId { get; init; } = string.Empty;

    [BsonElement("issuedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime IssuedAt { get; init; }

    // 绝对到期时间；TTL 索引只用于清理，是否过期以显式比较为准。
    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; init; }

    [BsonElement("consumedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ConsumedAt { get; init; }

    [BsonElement(FieldNames.RevokedAt)]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? RevokedAt { get; init; }

    [BsonElement("replacedByTokenId")]
    public string? ReplacedByTokenId { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static AppRefreshTokenDocument FromDomain(AppRefreshToken token) => new()
    {
        TokenId = token.TokenId,
        TokenHash = token.TokenHash,
        PrincipalType = WireEnum.ToWire(token.PrincipalType),
        EntityId = token.EntityId,
        FamilyId = token.FamilyId,
        IssuedAt = UtcDateTimes.ToStored(token.IssuedAt),
        ExpiresAt = UtcDateTimes.ToStored(token.ExpiresAt),
        ConsumedAt = token.ConsumedAt is null ? null : UtcDateTimes.ToStored(token.ConsumedAt.Value),
        RevokedAt = token.RevokedAt is null ? null : UtcDateTimes.ToStored(token.RevokedAt.Value),
        ReplacedByTokenId = token.ReplacedByTokenId,
        Version = token.Version
    };

    public AppRefreshToken ToDomain() => new(
        TokenId,
        TokenHash,
        StoredValues.ParseEnum<PrincipalType>(PrincipalType, "principalType"),
        EntityId,
        FamilyId,
        UtcDateTimes.FromStored(IssuedAt),
        UtcDateTimes.FromStored(ExpiresAt),
        ConsumedAt is null ? null : UtcDateTimes.FromStored(ConsumedAt.Value),
        RevokedAt is null ? null : UtcDateTimes.FromStored(RevokedAt.Value),
        ReplacedByTokenId,
        Version);

    public static class FieldNames
    {
        public const string RevokedAt = "revokedAt";
        public const string FamilyId = "familyId";
    }
}
