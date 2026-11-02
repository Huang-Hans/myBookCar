using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LMS.BuildingBlocks.Identity.Authorization;

// 让 MVC 过滤器用统一响应体（IResult）短路请求。
internal sealed class HttpResultActionResult(IResult result) : IActionResult
{
    public Task ExecuteResultAsync(ActionContext context) => result.ExecuteAsync(context.HttpContext);
}
