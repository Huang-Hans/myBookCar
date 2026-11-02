using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Results;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.BuildingBlocks.Identity.Authorization;

// 授权过滤器基类：解析调用者授权并检查权限，通过后把调用者放入请求上下文供 HttpContext.GetCaller 读取。
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class CallerAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
{
    // 为 null 时只要求调用者拥有可用授权。
    protected abstract string? RequiredPermission { get; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var resolver = http.RequestServices.GetRequiredService<ICallerAuthorizationResolver>();
        CallerAuthorization? caller;
        try
        {
            caller = await resolver.ResolveAsync(http, http.RequestAborted);
        }
        catch (CallerAuthorizationUnavailableException)
        {
            context.Result = Reject(ApiErrors.ServiceUnavailable);
            return;
        }

        if (!IsPermitted(caller))
        {
            context.Result = Reject(ApiErrors.InsufficientPermission);
            return;
        }

        http.SetCaller(caller!);
    }

    private bool IsPermitted(CallerAuthorization? caller) =>
        caller is not null && (RequiredPermission is null || caller.Has(RequiredPermission));

    private static HttpResultActionResult Reject(ApiError error) => new(ApiResults.Fail(error));
}
