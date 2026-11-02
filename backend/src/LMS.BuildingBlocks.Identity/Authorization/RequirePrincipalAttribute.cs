using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Results;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.BuildingBlocks.Identity.Authorization;

// 要求调用者是指定类型的司机/客户本人并拥有指定权限。员工授权与本人授权互不通用：
// 员工令牌访问本人端点、司机令牌访问客户端点都是 403/40300。
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePrincipalAttribute(PrincipalType type, string permission) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var resolver = http.RequestServices.GetRequiredService<IPrincipalAuthorizationResolver>();
        PrincipalAuthorization? principal;
        try
        {
            principal = await resolver.ResolveAsync(http, http.RequestAborted);
        }
        catch (CallerAuthorizationUnavailableException)
        {
            context.Result = Reject(ApiErrors.ServiceUnavailable);
            return;
        }

        if (principal is null || principal.Type != type || !principal.Has(permission))
        {
            context.Result = Reject(ApiErrors.InsufficientPermission);
            return;
        }

        http.SetPrincipal(principal);
    }

    private static HttpResultActionResult Reject(ApiError error) => new(ApiResults.Fail(error));
}
