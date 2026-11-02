using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Requests;

// 合同 NoBodyRequest：只有 path 参数和共有请求头。携带正文是请求结构错误（40000），
// 携带任何未声明的查询参数是校验错误（40001）。
public static class NoBodyRequest
{
    public static IResult? Validate(HttpContext context) => QueryReader.For(context).Failure();
}
