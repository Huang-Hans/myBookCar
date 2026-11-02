using LMS.BuildingBlocks.Http.Tracing;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Results;

// 延迟到执行时才创建响应体，确保响应体携带当前请求的 traceId。
internal sealed class ApiEnvelopeResult<TBody>(int statusCode, int code, Func<string, TBody> createBody) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext) =>
        ApiResponseWriter.WriteAsync(
            httpContext,
            statusCode,
            code,
            createBody(httpContext.GetTraceId()),
            httpContext.RequestAborted);
}
