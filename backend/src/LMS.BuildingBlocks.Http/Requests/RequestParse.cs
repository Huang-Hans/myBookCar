using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Requests;

public static class RequestParse
{
    // 按传入顺序返回第一个失败响应；全部成功时返回 null。
    public static IResult? FirstFailure(params IRequestParseResult[] parts) =>
        parts.Select(part => part.Failure).FirstOrDefault(failure => failure is not null);
}
