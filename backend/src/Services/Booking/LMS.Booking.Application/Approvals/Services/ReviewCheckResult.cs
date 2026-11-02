using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Application.Approvals.Services;

// 批准前跨域核对的结果：数据源不可用，或带有逐项问题；两者都没有则通过。
public sealed record ReviewCheckResult(bool IsAvailable, IReadOnlyList<FieldIssue> Issues)
{
    public static ReviewCheckResult Passed { get; } = new(true, []);

    public static ReviewCheckResult Unavailable { get; } = new(false, []);

    // 通过时为 null；否则是应直接给出的失败结果。
    public OperationResult<T>? ToFailure<T>()
    {
        if (!IsAvailable)
        {
            return OperationResult<T>.Failed(OperationOutcome.SourceUnavailable);
        }

        return Issues.Count > 0 ? OperationResult<T>.Rejected(Issues) : null;
    }
}
