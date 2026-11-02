using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Requests;

// 请求某一部分（正文、请求头、查询串）的解析结果：失败时携带可直接返回的失败响应。
public interface IRequestParseResult
{
    IResult? Failure { get; }
}
