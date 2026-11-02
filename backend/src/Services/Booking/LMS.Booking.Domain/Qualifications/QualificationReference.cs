namespace LMS.Booking.Domain.Qualifications;

// 外部资格与积分参考，加上本地的例外处理状态。升级和批准例外不修改外部结果、积分或权益参考。
// tripReference：控制台建单产生的关联（HA-09 审批与行程单的连接点）；独立检查时为空。
public sealed record QualificationReference(
    string QualificationId,
    GuestSnapshot Guest,
    string PropertyId,
    string LoyaltyOutcome,
    string QualificationReferenceCode,
    string EntitlementType,
    long? PointsBalance,
    long? PointsRequired,
    IReadOnlyList<string> ApprovalChain,
    QualificationStatus Status,
    string? Justification,
    string? OperatorId,
    DateTimeOffset? OperatedAt,
    long Version,
    string? TripReference = null,
    QualificationRequest? Request = null)
{
    public QualificationReference WithRequest(QualificationRequest request) => this with { Request = request };

    public QualificationReference WithOperation(QualificationStatus status, string justification, string operatorId, DateTimeOffset operatedAt) =>
        this with { Status = status, Justification = justification, OperatorId = operatorId, OperatedAt = operatedAt };
}
