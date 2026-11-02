using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Users.Services;

internal static class PortalUserChecks
{
    // 合并后的用户必须满足领域规则，且调用者有权授予该范围：
    // 全物业授权只能由全物业调用者授予；单一物业授权只能授予调用者自己可访问的物业。
    public static OperationResult<PortalUser> Evaluate(PortalUser candidate, PropertyScope callerScope)
    {
        var issues = PortalUserRules.Validate(candidate);
        if (issues.Count > 0)
        {
            return OperationResult<PortalUser>.Invalid(issues);
        }

        return IsGrantable(candidate, callerScope)
            ? OperationResult<PortalUser>.Succeeded(candidate)
            : OperationResult<PortalUser>.Failed(OperationOutcome.Forbidden);
    }

    private static bool IsGrantable(PortalUser target, PropertyScope callerScope) =>
        callerScope.IsAllProperties || (!target.HasAllPropertyAccess && target.PropertyId is not null && callerScope.Allows(target.PropertyId));
}
