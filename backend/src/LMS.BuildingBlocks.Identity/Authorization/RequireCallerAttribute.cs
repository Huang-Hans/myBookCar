namespace LMS.BuildingBlocks.Identity.Authorization;

// 只要求调用者关联了可用的应用用户。
public sealed class RequireCallerAttribute : CallerAuthorizationAttribute
{
    protected override string? RequiredPermission => null;
}
