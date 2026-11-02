namespace LMS.BuildingBlocks.Identity.Authorization;

// 要求调用者拥有指定权限；无权限为 403/40300。
public sealed class RequirePermissionAttribute(string permission) : CallerAuthorizationAttribute
{
    protected override string? RequiredPermission => permission;
}
