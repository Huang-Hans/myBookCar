using LMS.BuildingBlocks.Http.Contracts;

namespace LMS.BuildingBlocks.Http.Results;

// 失败 Envelope 的唯一构造入口：有字段错误时以第一条原因作为 message，便于前端直接展示具体原因。
internal static class ApiErrorEnvelopeFactory
{
    public static ApiErrorEnvelope Create(ApiError error, IReadOnlyList<ApiFieldError>? fieldErrors, string traceId)
    {
        var errors = fieldErrors ?? [];
        return new ApiErrorEnvelope(error.Code, ResolveMessage(error, errors), errors, traceId);
    }

    private static string ResolveMessage(ApiError error, IReadOnlyList<ApiFieldError> errors)
    {
        var firstMessage = errors.Count > 0 ? errors[0].Message : null;
        return string.IsNullOrWhiteSpace(firstMessage) ? error.Message : firstMessage;
    }
}
