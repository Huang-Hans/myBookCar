using LMS.BuildingBlocks.Http.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Errors;

// 为未处理异常写出失败响应体。异常本身由异常处理中间件在请求作用域内记录日志，
// 异常内容不会返回给客户端。
internal sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var error = ApiErrors.FromException(exception);
        await ApiResponseWriter.WriteErrorAsync(httpContext, error, null, cancellationToken);
        return true;
    }
}
