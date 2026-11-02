using System.Diagnostics;
using LMS.BuildingBlocks.Http.Tracing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LMS.ServiceDefaults.Middleware;

// 每个请求写一行结构化完成日志，并通过日志作用域为请求内的每行日志带上 serviceName 与 traceId。
// 只记录固定字段：不记录请求头、查询字符串和请求体。
internal sealed partial class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger,
    IHostEnvironment environment)
{
    private const string UnmatchedEndpoint = "unmatched";

    public async Task InvokeAsync(HttpContext context)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        using var scope = logger.BeginScope(CreateScope(context));
        try
        {
            await next(context);
        }
        finally
        {
            LogCompleted(context, Stopwatch.GetElapsedTime(startTimestamp));
        }
    }

    private Dictionary<string, object> CreateScope(HttpContext context) => new()
    {
        ["serviceName"] = environment.ApplicationName,
        ["traceId"] = context.GetTraceId(),
    };

    private void LogCompleted(HttpContext context, TimeSpan elapsed) =>
        LogRequestCompleted(
            logger,
            ResolveLevel(context),
            context.Request.Method,
            ResolveEndpoint(context),
            context.Response.StatusCode,
            context.GetApiCode(),
            (long)elapsed.TotalMilliseconds);

    private static LogLevel ResolveLevel(HttpContext context)
    {
        if (Extensions.IsProbePath(context.Request.Path))
        {
            return LogLevel.Debug;
        }

        return context.Response.StatusCode >= StatusCodes.Status500InternalServerError
            ? LogLevel.Warning
            : LogLevel.Information;
    }

    // 使用路由模板可保持该字段低基数，绝不记录原始 URL。
    private static string ResolveEndpoint(HttpContext context)
    {
        var endpoint = context.GetEndpoint() ?? context.Features.Get<IExceptionHandlerFeature>()?.Endpoint;
        return (endpoint as RouteEndpoint)?.RoutePattern.RawText ?? UnmatchedEndpoint;
    }

    [LoggerMessage(Message = "HTTP {method} {endpoint} responded {statusCode} (code {code}) in {elapsedMs} ms")]
    private static partial void LogRequestCompleted(
        ILogger logger,
        LogLevel level,
        string method,
        string endpoint,
        int statusCode,
        int? code,
        long elapsedMs);
}
