using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Application.Directory;

namespace LMS.Booking.Api.Authorization;

// 令牌由本服务验证；角色、物业范围和权限由 Platform 的 portal_users 解析。
// Platform 暂时不可用时返回 503，而不是把调用者误判为无权限。
internal sealed class PlatformCallerAuthorizationResolver(IPlatformDirectory directory) : ICallerAuthorizationResolver
{
    public async Task<CallerAuthorization?> ResolveAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var lookup = await directory.ResolveCallerAsync(cancellationToken);

        return lookup.Outcome switch
        {
            CallerLookupOutcome.Found => ToAuthorization(lookup.Caller!),
            CallerLookupOutcome.NotLinked => null,
            _ => throw new CallerAuthorizationUnavailableException("The Platform authorization source is unavailable.")
        };
    }

    private static CallerAuthorization ToAuthorization(PlatformCaller caller) =>
        new(caller.UserId, caller.RoleCode, caller.HasAllPropertyAccess, caller.PropertyId, caller.Permissions, caller.Name);
}
