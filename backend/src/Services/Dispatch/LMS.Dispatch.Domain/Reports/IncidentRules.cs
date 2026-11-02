using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Dispatch.Domain.Reports;

// 事故报告的字段规则：发生时间不得晚于服务端当前时间（容忍小幅时钟偏差）。补报期限（Q2-02）未确认，因此不设下限。
public static class IncidentRules
{
    public static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);

    public static IReadOnlyList<FieldIssue> Validate(DateTimeOffset occurredAt, DateTimeOffset now) =>
        occurredAt > now + ClockTolerance
            ? [new FieldIssue("occurredAt", "Must not be in the future")]
            : [];
}
