using LMS.BuildingBlocks.Http.Contracts;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Results;

// 供身份认证等管线内组件直接写出失败 Envelope（这些位置拿不到 IResult 的执行时机）。
public static class ApiErrorWriter
{
    public static Task WriteAsync(HttpContext context, ApiError error) =>
        ApiResponseWriter.WriteErrorAsync(context, error, null, context.RequestAborted);
}
