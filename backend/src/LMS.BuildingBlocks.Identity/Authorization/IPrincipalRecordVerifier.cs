using LMS.BuildingBlocks.Core.Authorization;

namespace LMS.BuildingBlocks.Identity.Authorization;

// 令牌验签之后，由数据所属服务确认令牌 sub 对应的司机/客户记录仍然存在且可用。
// 本服务不拥有该记录时使用默认实现（只信任已验签令牌），数据归属由本人范围查询保证。
public interface IPrincipalRecordVerifier
{
    // 记录不存在或已不可用返回 false；数据源暂时不可用时抛出 CallerAuthorizationUnavailableException。
    Task<bool> IsUsableAsync(PrincipalType type, string entityId, CancellationToken cancellationToken);
}
