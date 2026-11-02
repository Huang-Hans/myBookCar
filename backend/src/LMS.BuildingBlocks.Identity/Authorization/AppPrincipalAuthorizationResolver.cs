using System.Security.Claims;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Identity.Authorization;

// 从已通过 LmsAppBearer 验证的令牌得到司机/客户本人授权：主体类型取自令牌的 principalType（登录入口固定），
// 业务实体标识取自 sub（driverId / guestId）。权限随主体类型固定给出，请求头、路径或正文里的标识一律不参与。
internal sealed class AppPrincipalAuthorizationResolver(IPrincipalRecordVerifier verifier) : IPrincipalAuthorizationResolver
{
    public async Task<PrincipalAuthorization?> ResolveAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var entityId = context.User.FindFirstValue(AppTokenClaims.Subject);
        var typeText = context.User.FindFirstValue(AppTokenClaims.PrincipalType);
        if (string.IsNullOrWhiteSpace(entityId) || !WireEnum.TryParse<PrincipalType>(typeText, out var type))
        {
            return null;
        }

        return await verifier.IsUsableAsync(type, entityId, cancellationToken)
            ? new PrincipalAuthorization(type, entityId, PrincipalPermissionCodes.For(type))
            : null;
    }
}
