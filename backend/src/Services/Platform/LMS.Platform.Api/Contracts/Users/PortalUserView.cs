namespace LMS.Platform.Api.Contracts.Users;

/// <summary>
/// 门户用户视图，映射 portal_users；空值字段省略。
/// </summary>
/// <param name="UserId">应用用户标识。</param>
/// <param name="Name">用户姓名。</param>
/// <param name="CorporateEmail">企业邮箱，不区分大小写去重并保留规范化值。</param>
/// <param name="RoleCode">角色代码，SUPER_ADMIN / OPERATIONS_MANAGER / DISPATCHER / FINANCE / PROPERTY_CS_STAFF。</param>
/// <param name="HasAllPropertyAccess">是否拥有全部物业访问授权。</param>
/// <param name="PropertyId">单一物业代码；全物业授权时为空并省略该键。</param>
/// <param name="IsActive">用户启用状态。</param>
/// <param name="IdentityProvider">企业 SSO 来源代码，非客户端可写。</param>
/// <param name="Version">乐观锁版本号，配合 X-Expected-Version 头做并发控制。</param>
public sealed record PortalUserView(
    string UserId,
    string Name,
    string CorporateEmail,
    string RoleCode,
    bool HasAllPropertyAccess,
    string? PropertyId,
    bool IsActive,
    string IdentityProvider,
    long Version);
