using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Tracing;
using Microsoft.AspNetCore.Http;

namespace LMS.ServiceDefaults.Middleware;

// 每个请求只解析一次 traceId，并在所有响应的 X-Trace-Id 头中回写。
internal sealed class TraceIdMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var traceId = context.GetTraceId();
        context.Response.OnStarting(WriteTraceIdHeader, (context.Response, traceId));
        return next(context);
    }

    // 采用赋值而非追加，即使被代理的响应已带有该头，也保证只有一个值。
    private static Task WriteTraceIdHeader(object state)
    {
        var (response, traceId) = ((HttpResponse, string))state;
        response.Headers[ApiHeaderNames.TraceId] = traceId;
        return Task.CompletedTask;
    }
}
