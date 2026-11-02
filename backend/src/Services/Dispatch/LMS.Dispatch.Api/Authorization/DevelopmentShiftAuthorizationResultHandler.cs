using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace LMS.Dispatch.Api.Authorization;

// 令牌缺失时，JwtBearer 会先把 401 写进响应。这里只负责在认证被跳过之后放行员工接口。
internal sealed class DevelopmentShiftAuthorizationResultHandler(
    ILogger<DevelopmentShiftAuthorizationResultHandler> logger) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();
    private int announced;

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded || !DevelopmentStaffAccess.Allows(context))
        {
            await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        if (Interlocked.Exchange(ref announced, 1) == 0)
        {
            logger.LogWarning("Development access: staff APIs accept requests without a bearer token.");
        }

        await next(context);
    }
}
