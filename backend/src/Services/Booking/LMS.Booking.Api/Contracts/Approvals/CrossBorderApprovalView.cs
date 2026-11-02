using LMS.Booking.Api.Contracts.Qualifications;

namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 跨境审批详情：审核记录及其客人、路线、权益、材料与决定快照。
/// </summary>
/// <param name="ApprovalId">审核记录标识。</param>
/// <param name="TripReference">外部行程引用，形如 XB-...。</param>
/// <param name="Guest">客人与会员等级快照。</param>
/// <param name="PropertyId">审核所属物业代码。</param>
/// <param name="Route">路线快照，包含取货点与送达点名称。</param>
/// <param name="PickupAt">取货时间，UTC 时间点。</param>
/// <param name="SlaDeadlineAt">SLA 截止时刻，UTC 时间点；无 SLA 要求时为空并省略该键。</param>
/// <param name="AssignedDriverId">指派司机标识；未指派时为空并省略该键，不能按司机姓名关联。</param>
/// <param name="AssignedVehicle">行程车辆描述；未指派时为空并省略该键。</param>
/// <param name="ApprovalChain">原型展示的有序审批阶段标签；不是 roleCode，不用于授权。</param>
/// <param name="Loyalty">兑换与权益参考快照。</param>
/// <param name="Documents">审核材料列表，每种类型恰好一项，不接受额外材料类型。</param>
/// <param name="Status">审核状态，取大写下划线字面值 PENDING、APPROVED 或 REJECTED。</param>
/// <param name="Decision">最终审核结果；尚未决定时为空并省略该键，决定评语必填。</param>
/// <param name="Version">乐观锁版本号，配合 X-Expected-Version 头做并发控制。</param>
public sealed record CrossBorderApprovalView(
    string ApprovalId,
    string TripReference,
    GuestSnapshotView Guest,
    string PropertyId,
    RouteSnapshotView Route,
    DateTime PickupAt,
    DateTime? SlaDeadlineAt,
    string? AssignedDriverId,
    string? AssignedVehicle,
    IReadOnlyList<string> ApprovalChain,
    LoyaltyReferenceView Loyalty,
    IReadOnlyList<ApprovalDocumentView> Documents,
    string Status,
    ApprovalDecisionView? Decision,
    long Version);
