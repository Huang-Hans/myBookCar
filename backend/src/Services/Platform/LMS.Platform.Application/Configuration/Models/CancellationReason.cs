namespace LMS.Platform.Application.Configuration.Models;

public sealed record CancellationReason(
    string ReasonCode,
    string Name,
    string PointsReturnPolicyCode,
    string PointsReturnReference,
    string Source);
