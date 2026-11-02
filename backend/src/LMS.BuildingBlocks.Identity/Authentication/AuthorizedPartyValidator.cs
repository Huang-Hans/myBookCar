using System.Security.Claims;
using LMS.BuildingBlocks.Identity.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace LMS.BuildingBlocks.Identity.Authentication;

internal static class AuthorizedPartyValidator
{
    public const string ClaimType = "azp";

    // 访问令牌必须由允许的客户端申请（azp）。缺少 azp 或客户端不在名单内时认证失败，最终表现为 40100。
    public static Task ValidateAsync(TokenValidatedContext context, IdentityOptions options)
    {
        var authorizedParty = context.Principal?.FindFirstValue(ClaimType);
        var isAllowed = authorizedParty is not null && options.AllowedClients.Contains(authorizedParty, StringComparer.Ordinal);
        if (!isAllowed)
        {
            context.Fail("The token was issued to a client that is not allowed.");
        }

        return Task.CompletedTask;
    }
}
