using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Identity.Authorization;

namespace LMS.Dispatch.Api.Authorization;

// 没有令牌时，员工接口使用一个仅存在于本进程的调用者，物业范围为全部。
internal sealed class DevelopmentCallerAuthorizationResolver(PlatformCallerAuthorizationResolver platform) : ICallerAuthorizationResolver
{
    private static readonly CallerAuthorization DevelopmentCaller = new(
        "dev-swagger",
        "SUPER_ADMIN",
        true,
        null,
        PermissionCodes.All,
        "Swagger");

    public Task<CallerAuthorization?> ResolveAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (DevelopmentStaffAccess.Allows(context))
        {
            return Task.FromResult<CallerAuthorization?>(DevelopmentCaller);
        }

        return platform.ResolveAsync(context, cancellationToken);
    }
}
