using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Results;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Http.Requests;

// 收集字段级校验错误，统一输出 40001。
public sealed class FieldErrorCollector
{
    private readonly List<ApiFieldError> errors = [];

    public bool HasErrors => errors.Count > 0;

    public IReadOnlyList<ApiFieldError> Errors => errors;

    public void Add(string field, string message) => errors.Add(new ApiFieldError(field, message));

    public void AddRange(IEnumerable<FieldIssue> issues)
    {
        foreach (var issue in issues)
        {
            Add(issue.Field, issue.Message);
        }
    }

    public IResult ToFailure() => ApiResults.ValidationFailed(errors);
}
