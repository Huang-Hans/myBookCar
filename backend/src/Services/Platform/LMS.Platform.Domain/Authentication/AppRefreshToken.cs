using LMS.BuildingBlocks.Core.Authorization;

namespace LMS.Platform.Domain.Authentication;

// 司机/客户共用的刷新凭据记录：只保存认证会话与刷新事实，不是账号表。TokenHash 为高熵随机凭据的摘要，
// PrincipalType + EntityId 在整条轮换链上保持不变（司机凭据不能换取客户令牌）。同一次登录的所有凭据共享 FamilyId。
// ExpiresAt 是登录后的绝对到期时间，轮换不会延长。
public sealed record AppRefreshToken(
    string TokenId,
    string TokenHash,
    PrincipalType PrincipalType,
    string EntityId,
    string FamilyId,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConsumedAt,
    DateTimeOffset? RevokedAt,
    string? ReplacedByTokenId,
    long Version)
{
    public const string TokenIdPrefix = "rt_";
    public const string FamilyIdPrefix = "fam_";

    private const long FirstVersion = 1;

    public static string NewFamilyId() => FamilyIdPrefix + Guid.CreateVersion7().ToString("N");

    public static AppRefreshToken Issue(
        string tokenHash,
        PrincipalType principalType,
        string entityId,
        string familyId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt) =>
        new(
            TokenIdPrefix + Guid.CreateVersion7().ToString("N"),
            tokenHash, principalType, entityId, familyId, issuedAt, expiresAt, null, null, null, FirstVersion);

    public bool IsExpiredAt(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsUsableAt(DateTimeOffset now) => ConsumedAt is null && RevokedAt is null && !IsExpiredAt(now);

    public AppRefreshToken Consume(string replacedByTokenId, DateTimeOffset now) =>
        this with { ConsumedAt = now, ReplacedByTokenId = replacedByTokenId };
}
