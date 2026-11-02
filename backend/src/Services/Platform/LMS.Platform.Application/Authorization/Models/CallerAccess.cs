namespace LMS.Platform.Application.Authorization.Models;

// 已验证主体对应的应用用户授权：角色、物业范围和生效权限；Name 是用户姓名，供下游保存经办人快照。
public sealed record CallerAccess(
    string UserId,
    string RoleCode,
    bool HasAllPropertyAccess,
    string? PropertyId,
    IReadOnlyCollection<string> Permissions,
    string? Name = null);
