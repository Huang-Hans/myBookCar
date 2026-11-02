using System.Globalization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Results;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Requests;

// 读取合同第 2.2 节的共有请求头。缺失或格式错误属于请求本身无法接受，返回 40000。
public static class RequestHeaders
{
    public const string ExpectedVersion = "X-Expected-Version";
    public const string IdempotencyKey = "Idempotency-Key";

    // 乐观并发暂时取消：请求头可选，写入以服务端读到的当前版本为准。缺失或格式不符时返回 0，不参与比较。
    public static RequestParseResult<long> ReadExpectedVersion(HttpContext context)
    {
        var text = context.Request.Headers[ExpectedVersion].ToString();
        var isValid = long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var version) && version >= 1;

        return RequestParseResult<long>.Success(isValid ? version : 0);
    }

    public static RequestParseResult<string> ReadIdempotencyKey(HttpContext context)
    {
        var key = context.Request.Headers[IdempotencyKey].ToString();

        return string.IsNullOrWhiteSpace(key)
            ? RequestParseResult<string>.Fail(Malformed(IdempotencyKey, "Is required"))
            : RequestParseResult<string>.Success(key.Trim());
    }

    private static IResult Malformed(string header, string message) =>
        ApiResults.Fail(ApiErrors.MalformedRequest, [new ApiFieldError(header, message)]);
}
