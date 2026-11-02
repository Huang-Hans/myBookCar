using LMS.Platform.Application.Authorization.Models;

namespace LMS.Platform.Application.Authorization.Handlers;

public interface ILinkCallerIdentityHandler
{
    // 登录时调用：主体已关联时还要求令牌中已验证的企业邮箱与 LMS 记录一致；尚未关联时按已验证企业邮箱一次性关联未关联的启用用户。
    // 返回 null 表示该主体在 LMS 中没有可用授权。
    Task<CallerAccess?> HandleAsync(CallerLookup lookup, CancellationToken cancellationToken);
}
