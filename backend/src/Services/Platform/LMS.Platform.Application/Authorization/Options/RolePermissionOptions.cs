namespace LMS.Platform.Application.Authorization.Options;

// 角色到操作权限的部署配置（Q01 尚未确认权限矩阵）。键为角色字面值，值为 PermissionCodes 中的权限。
// SUPER_ADMIN 固定拥有全部权限；其余角色未配置时没有任何操作权限。
public sealed class RolePermissionOptions
{
    public const string SectionName = "Authorization";

    public Dictionary<string, string[]> RolePermissions { get; init; } = new(StringComparer.Ordinal);
}
