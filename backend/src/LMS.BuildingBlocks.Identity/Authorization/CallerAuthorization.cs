using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.BuildingBlocks.Identity.Authorization;

// 解析后的调用者授权：应用用户、角色、物业范围和生效权限。来自持久化授权，不来自请求参数。
public sealed record CallerAuthorization(
    string UserId,
    string RoleCode,
    bool HasAllPropertyAccess,
    string? PropertyId,
    IReadOnlyCollection<string> Permissions,
    string? Name = null)
{
    public PropertyScope Scope => HasAllPropertyAccess ? PropertyScope.AllProperties : PropertyScope.ForProperty(PropertyId!);

    public bool Has(string permission) => Permissions.Contains(permission);
}
