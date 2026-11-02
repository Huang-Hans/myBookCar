using LMS.BuildingBlocks.Core.Authorization;

namespace LMS.BuildingBlocks.Identity.Authorization;

// 解析后的司机/客户本人授权：主体类型、业务实体标识（driverId / guestId）与生效权限。来自持久化绑定，不来自请求参数。
public sealed record PrincipalAuthorization(
    PrincipalType Type,
    string EntityId,
    IReadOnlyCollection<string> Permissions)
{
    public bool Has(string permission) => Permissions.Contains(permission);
}
