namespace LMS.Reporting.Infrastructure.Sources.Payloads;

// Platform GET internal/v1/reporting/cancellation-reasons 的一条取消原因。
internal sealed record PlatformCancellationReasonPayload(string ReasonCode, string Name, string PointsReturnPolicyCode, string PointsReturnReference);
