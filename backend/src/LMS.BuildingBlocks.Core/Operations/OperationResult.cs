namespace LMS.BuildingBlocks.Core.Operations;

public sealed record OperationResult<T>(OperationOutcome Outcome, T? Value, IReadOnlyList<FieldIssue> Issues)
{
    private static readonly IReadOnlyList<FieldIssue> NoIssues = [];

    public bool IsSuccess => Outcome is OperationOutcome.Succeeded or OperationOutcome.Created or OperationOutcome.Replayed;

    public static OperationResult<T> Succeeded(T value) => new(OperationOutcome.Succeeded, value, NoIssues);

    public static OperationResult<T> Created(T value) => new(OperationOutcome.Created, value, NoIssues);

    public static OperationResult<T> Replayed(T value) => new(OperationOutcome.Replayed, value, NoIssues);

    public static OperationResult<T> Failed(OperationOutcome outcome) => new(outcome, default, NoIssues);

    public static OperationResult<T> Invalid(IReadOnlyList<FieldIssue> issues) =>
        new(OperationOutcome.ValidationFailed, default, issues);

    public static OperationResult<T> Rejected(IReadOnlyList<FieldIssue> issues) =>
        new(OperationOutcome.BusinessRuleRejected, default, issues);

    // 把另一个结果的失败原样搬到本类型（结果分类与字段问题都保持不变），用于校验链中间结果的向外传递。
    public static OperationResult<T> FailureOf<TOther>(OperationResult<TOther> source) => new(source.Outcome, default, source.Issues);
}
