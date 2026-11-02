namespace LMS.Reporting.Domain.Rows;

public sealed record CancellationPayload(
    string ReasonCode,
    string Reason,
    string Source,
    long? NoticeSecondsBeforePickup,
    string? PointsReturnPolicyCode,
    string? PointsReturnReference) : ReportPayload;
