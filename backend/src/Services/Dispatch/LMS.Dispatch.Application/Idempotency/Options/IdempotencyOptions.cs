namespace LMS.Dispatch.Application.Idempotency.Options;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    // 必须覆盖客户端支持的最长重试窗口。
    public int RetentionHours { get; init; } = 24;
}
