using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Results;

// 把业务用例的结果分类映射为 HTTP 状态与应用码（合同第 2.4 节）。
public static class OperationResultMapper
{
    public static IResult ToResult<TValue, TView>(OperationResult<TValue> result, Func<TValue, TView> toView) =>
        result.Outcome switch
        {
            OperationOutcome.Succeeded => ApiResults.Ok(toView(result.Value!)),
            OperationOutcome.Created or OperationOutcome.Replayed => ApiResults.Created(toView(result.Value!)),
            _ => ToFailure(result)
        };

    // 异步任务的申请：成功与幂等重放都是 202，只表示已受理；任务状态由单独的读取接口给出。
    public static IResult ToAcceptedResult<TValue, TView>(OperationResult<TValue> result, Func<TValue, TView> toView) =>
        result.IsSuccess ? ApiResults.Accepted(toView(result.Value!)) : ToFailure(result);

    // 更新或决定类操作：成功与幂等重放都是 200（合同中这类操作的成功状态），不是 201。
    public static IResult ToUpdatedResult<TValue, TView>(OperationResult<TValue> result, Func<TValue, TView> toView) =>
        result.IsSuccess ? ApiResults.Ok(toView(result.Value!)) : ToFailure(result);

    public static IResult ToFailure<TValue>(OperationResult<TValue> result) =>
        result.Outcome switch
        {
            OperationOutcome.NotFound => ApiResults.Fail(ApiErrors.ResourceNotFound),
            OperationOutcome.StateConflict => Conflict(string.Empty, "The resource is not in a state that allows this operation"),
            OperationOutcome.VersionConflict => Conflict("X-Expected-Version", "Does not match the current version"),
            OperationOutcome.DuplicateConflict => Conflict(string.Empty, "A record with the same unique value already exists"),
            OperationOutcome.IdempotencyKeyReused => Conflict("Idempotency-Key", "Was already used with a different request"),
            OperationOutcome.ValidationFailed => ApiResults.ValidationFailed(ToFieldErrors(result.Issues)),
            OperationOutcome.BusinessRuleRejected => ApiResults.Fail(ApiErrors.BusinessRuleRejected, ToFieldErrors(result.Issues)),
            OperationOutcome.Forbidden => ApiResults.Fail(ApiErrors.InsufficientPermission),
            OperationOutcome.SourceUnavailable => ApiResults.Fail(ApiErrors.ServiceUnavailable),
            _ => throw new InvalidOperationException($"Outcome {result.Outcome} is not a failure.")
        };

    private static IResult Conflict(string field, string message) =>
        ApiResults.Fail(ApiErrors.StateConflict, [new ApiFieldError(field, message)]);

    private static List<ApiFieldError> ToFieldErrors(IReadOnlyList<FieldIssue> issues) =>
        issues.Select(issue => new ApiFieldError(issue.Field, issue.Message)).ToList();
}
