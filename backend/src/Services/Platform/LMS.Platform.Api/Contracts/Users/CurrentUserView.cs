namespace LMS.Platform.Api.Contracts.Users;

/// <summary>
/// 当前登录用户：令牌所属调用者自己的应用用户档案与生效授权，用于门户头部展示与按权限显示菜单。
/// </summary>
/// <param name="UserId">应用用户标识。</param>
/// <param name="Name">姓名。</param>
/// <param name="CorporateEmail">企业邮箱。</param>
/// <param name="RoleCode">角色代码。</param>
/// <param name="HasAllPropertyAccess">是否拥有全部物业的访问授权。</param>
/// <param name="PropertyId">单一物业代码；全物业授权时为空并省略。</param>
/// <param name="Permissions">当前生效的操作权限代码，来自角色权限配置，按字母顺序排列。</param>
public sealed record CurrentUserView(
    string UserId,
    string Name,
    string CorporateEmail,
    string RoleCode,
    bool HasAllPropertyAccess,
    string? PropertyId,
    IReadOnlyList<string> Permissions);
