using LMS.BuildingBlocks.Http.Tracing;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Application.Abstractions;

namespace LMS.Platform.Api.Controllers.Support;

internal static class CallerContexts
{
    // 写操作的调用上下文：操作人、物业范围来自已解析的授权，traceId 来自当前请求。
    public static CommandContext From(HttpContext http)
    {
        var caller = http.GetCaller();

        return new CommandContext(caller.UserId, http.GetTraceId(), caller.Scope);
    }
}
