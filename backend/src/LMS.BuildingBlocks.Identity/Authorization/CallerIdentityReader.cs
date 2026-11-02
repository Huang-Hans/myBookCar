using System.Security.Claims;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Identity.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LMS.BuildingBlocks.Identity.Authorization;

public static class CallerIdentityReader
{
    // 只读取员工（StaffBearer 企业令牌）的稳定主体。携带本地身份类型声明的令牌属于司机/客户/服务，
    // 不按企业来源代码解析，避免本地账号被当成员工主体。令牌已通过认证但缺少主体声明时返回 null，调用方应按无权限处理。
    public static CallerIdentity? Read(HttpContext context)
    {
        if (context.User.HasClaim(claim => claim.Type == AppTokenClaims.PrincipalType))
        {
            return null;
        }

        var options = context.RequestServices.GetRequiredService<IOptions<IdentityOptions>>().Value;
        var subject = context.User.FindFirstValue(options.SubjectClaimType);

        return string.IsNullOrWhiteSpace(subject)
            ? null
            : new CallerIdentity(options.ProviderCode, subject, ReadVerifiedEmail(context.User, options));
    }

    // 缺少邮箱声明，或发行方未确认邮箱已验证时返回 null。
    // EmailVerifiedClaimType 为空表示信任该发行方的邮箱（须经书面确认）。
    private static string? ReadVerifiedEmail(ClaimsPrincipal user, IdentityOptions options)
    {
        var email = user.FindFirstValue(options.EmailClaimType);

        return !string.IsNullOrWhiteSpace(email) && IsEmailVerified(user, options) ? email : null;
    }

    private static bool IsEmailVerified(ClaimsPrincipal user, IdentityOptions options) =>
        string.IsNullOrWhiteSpace(options.EmailVerifiedClaimType)
        || string.Equals(user.FindFirstValue(options.EmailVerifiedClaimType), bool.TrueString, StringComparison.OrdinalIgnoreCase);
}
