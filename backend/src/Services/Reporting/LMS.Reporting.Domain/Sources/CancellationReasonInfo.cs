namespace LMS.Reporting.Domain.Sources;

// Platform 当前生效的取消原因：名称与积分退还政策引用。
public sealed record CancellationReasonInfo(string ReasonCode, string Name, string PointsReturnPolicyCode, string PointsReturnReference);
