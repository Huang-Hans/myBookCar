using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Tracing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace LMS.BuildingBlocks.Http.Results;

internal static class ApiResponseWriter
{
    public static Task WriteErrorAsync(
        HttpContext context,
        ApiError error,
        IReadOnlyList<ApiFieldError>? fieldErrors,
        CancellationToken cancellationToken)
    {
        var body = ApiErrorEnvelopeFactory.Create(error, fieldErrors, context.GetTraceId());
        return WriteAsync(context, error.StatusCode, error.Code, body, cancellationToken);
    }

    public static Task WriteAsync<TBody>(
        HttpContext context,
        int statusCode,
        int code,
        TBody body,
        CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        context.SetApiCode(code);
        ApplyDefaultCacheControl(context);
        return context.Response.WriteAsJsonAsync(body, cancellationToken);
    }

    // 响应体是每次请求各自的数据，因此除非处理程序另有说明，GET 响应默认不可缓存。
    private static void ApplyDefaultCacheControl(HttpContext context)
    {
        var headers = context.Response.Headers;
        if (HttpMethods.IsGet(context.Request.Method) && StringValues.IsNullOrEmpty(headers.CacheControl))
        {
            headers.CacheControl = "no-store";
        }
    }
}
