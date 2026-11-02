using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Application.Directory;

// 对 Platform / Dispatch 的服务间读取结果。
public sealed record DirectoryResult<T>(DirectoryOutcome Outcome, T? Value, IReadOnlyList<FieldIssue> Issues)
{
    private static readonly IReadOnlyList<FieldIssue> NoIssues = [];

    public bool IsSuccess => Outcome == DirectoryOutcome.Success;

    public static DirectoryResult<T> Success(T value) => new(DirectoryOutcome.Success, value, NoIssues);

    public static DirectoryResult<T> Failed(DirectoryOutcome outcome) => new(outcome, default, NoIssues);

    public static DirectoryResult<T> Rejected(IReadOnlyList<FieldIssue> issues) => new(DirectoryOutcome.Rejected, default, issues);
}
