using LMS.BuildingBlocks.Http.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Errors;

// StatusCodePages 只对没有响应体或内容类型的响应生效，因此下游服务已生成的响应体不会被二次包装。
internal static class ApiStatusCodeResponses
{
    public static Task WriteAsync(StatusCodeContext statusCodeContext)
    {
        var context = statusCodeContext.HttpContext;
        if (!AllowsResponseBody(context.Request))
        {
            return Task.CompletedTask;
        }

        var error = ApiErrors.FromStatusCode(context.Response.StatusCode);
        return ApiResponseWriter.WriteErrorAsync(context, error, null, context.RequestAborted);
    }

    private static bool AllowsResponseBody(HttpRequest request) =>
        !HttpMethods.IsHead(request.Method) && !HttpMethods.IsOptions(request.Method);
}
