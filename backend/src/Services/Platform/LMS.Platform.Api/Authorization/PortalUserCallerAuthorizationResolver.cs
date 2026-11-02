using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Application.Authorization.Handlers;

namespace LMS.Platform.Api.Authorization;

// 把已验证令牌的稳定主体关联到 portal_users，得到角色、物业范围和生效权限。
// 请求头里的 userId / role / property 一律不参与授权。
internal sealed class PortalUserCallerAuthorizationResolver(IResolveCallerHandler handler) : ICallerAuthorizationResolver
{
    public async Task<CallerAuthorization?> ResolveAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var identity = CallerIdentityReader.Read(context);
        if (identity is null)
        {
            return null;
        }

        var access = await handler.HandleAsync(identity.ProviderCode, identity.SubjectId, cancellationToken);

        return access is null
            ? null
            : new CallerAuthorization(access.UserId, access.RoleCode, access.HasAllPropertyAccess, access.PropertyId, access.Permissions, access.Name);
    }
}
