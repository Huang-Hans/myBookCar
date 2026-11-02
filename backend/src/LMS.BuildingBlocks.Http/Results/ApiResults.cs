using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Results;

public static class ApiResults
{
    private const string SuccessMessage = "success";

    public static IResult Ok<T>(T data) =>
        new ApiEnvelopeResult<ApiEnvelope<T>>(
            StatusCodes.Status200OK,
            ApiCodes.Success,
            traceId => new ApiEnvelope<T>(ApiCodes.Success, SuccessMessage, data, null, traceId));

    // 分页列表：meta 同时携带 traceId 与统一分页结构。
    public static IResult OkPage<T>(IReadOnlyList<T> items, ApiPagination pagination) =>
        new ApiEnvelopeResult<ApiEnvelope<IReadOnlyList<T>>>(
            StatusCodes.Status200OK,
            ApiCodes.Success,
            traceId => new ApiEnvelope<IReadOnlyList<T>>(
                ApiCodes.Success, SuccessMessage, items, new ApiMeta(traceId, pagination), traceId));

    public static IResult Created<T>(T data) =>
        new ApiEnvelopeResult<ApiEnvelope<T>>(
            StatusCodes.Status201Created,
            ApiCodes.Created,
            traceId => new ApiEnvelope<T>(ApiCodes.Created, SuccessMessage, data, null, traceId));

    // 仅表示受理（202）：任务尚未完成，结果需要另行读取。
    public static IResult Accepted<T>(T data) =>
        new ApiEnvelopeResult<ApiEnvelope<T>>(
            StatusCodes.Status202Accepted,
            ApiCodes.Accepted,
            traceId => new ApiEnvelope<T>(ApiCodes.Accepted, SuccessMessage, data, null, traceId));

    public static IResult Fail(ApiError error, IReadOnlyList<ApiFieldError>? fieldErrors = null) =>
        new ApiEnvelopeResult<ApiErrorEnvelope>(
            error.StatusCode,
            error.Code,
            traceId => ApiErrorEnvelopeFactory.Create(error, fieldErrors, traceId));

    public static IResult ValidationFailed(IReadOnlyList<ApiFieldError> fieldErrors) =>
        Fail(ApiErrors.ValidationFailed, fieldErrors);
}
