namespace LMS.Booking.Api.Contracts.Qualifications;

/// <summary>
/// 本地资格引用记录的当前状态：客人快照、外部资格结果、审批链路与最近操作信息。
/// </summary>
/// <param name="QualificationId">本地资格引用记录标识。</param>
/// <param name="Guest">客人标识、姓名与会员等级快照。</param>
/// <param name="PropertyId">资格引用所属物业代码。</param>
/// <param name="LoyaltyOutcome">外部资格结果标签，不由本地积分推算。</param>
/// <param name="QualificationReference">外部忠诚度系统的资格引用。</param>
/// <param name="EntitlementType">权益类型。</param>
/// <param name="PointsBalance">外部可用积分参考值，非负，不在本地扣减；未知时为空并省略该键。</param>
/// <param name="PointsRequired">外部所需积分参考值，非负；未知时为空并省略该键。</param>
/// <param name="ApprovalChain">原型展示的有序审批阶段标签，当前为 Property CS → Operations Manager；不是 roleCode，不用于授权。</param>
/// <param name="Status">资格状态，取 ELIGIBLE、MANAGER_APPROVAL_REQUIRED、ESCALATED（待经理决定）、APPROVED 或 REJECTED。</param>
/// <param name="Justification">升级或批准例外的说明；未提交时为空并省略该键。</param>
/// <param name="OperatorId">最近核验、升级或批准的操作主体；尚未发生时为空并省略该键。</param>
/// <param name="OperatedAt">对应的操作时间，UTC 时间点；尚未发生时为空并省略该键。</param>
/// <param name="Version">版本号。X-Expected-Version 请求头可选，目前不参与比较。</param>
/// <param name="Request">经理复审的申请快照；尚无人提出申请（ELIGIBLE / MANAGER_APPROVAL_REQUIRED 的存量记录）时省略。</param>
/// <param name="Decision">经理的决定；仅 APPROVED / REJECTED 时存在。</param>
public sealed record QualificationView(
    string QualificationId,
    GuestSnapshotView Guest,
    string PropertyId,
    string LoyaltyOutcome,
    string QualificationReference,
    string EntitlementType,
    long? PointsBalance,
    long? PointsRequired,
    IReadOnlyList<string> ApprovalChain,
    string Status,
    string? Justification,
    string? OperatorId,
    DateTime? OperatedAt,
    long Version,
    QualificationRequestView? Request = null,
    QualificationDecisionView? Decision = null);
