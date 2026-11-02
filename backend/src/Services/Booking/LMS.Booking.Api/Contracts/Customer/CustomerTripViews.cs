using LMS.Booking.Api.Contracts.Trips;

namespace LMS.Booking.Api.Contracts.Customer;

/// <summary>
/// 客户订单列表项（订单页签与首页当前旅程共用）。
/// </summary>
/// <param name="TripId">订单（行程）标识。</param>
/// <param name="TripReference">订单业务单号。</param>
/// <param name="ProductCode">服务产品：BUSINESS_SEDAN / LIMOUSINE / PARTY / CROSS_BORDER_TRANSFER；客服代订的订单没有产品，省略。</param>
/// <param name="ServiceType">履约行程类型。</param>
/// <param name="Status">后端主状态（八态）。</param>
/// <param name="Group">订单分组：ACTIVE（全部非终态）/ COMPLETED / CANCELLED。</param>
/// <param name="Stage">细分阶段，客户端据此显示简化状态；新订单一律为 CONFIRMED；PENDING 只出现在存量订单上，不应显示为已确认。</param>
/// <param name="VehicleClass">车型产品，不是实际派出的车牌。</param>
/// <param name="Passengers">乘客人数。</param>
/// <param name="Route">起终点（目录 ID 与名称快照）。</param>
/// <param name="PickupAt">计划接车时间（UTC）。</param>
/// <param name="CreatedAt">下单时间（UTC），不用计划接车时间替代。</param>
/// <param name="CanCancel">当前是否可由客户取消，由服务端状态与取消窗口决定。</param>
/// <param name="Version">订单版本号。</param>
public sealed record CustomerTripSummaryView(
    string TripId,
    string TripReference,
    string? ProductCode,
    string ServiceType,
    string Status,
    string Group,
    string Stage,
    string VehicleClass,
    int Passengers,
    TripRouteView Route,
    DateTimeOffset PickupAt,
    DateTimeOffset CreatedAt,
    bool CanCancel,
    long Version);

/// <summary>Driver and vehicle summary. Contact is available only while the assigned trip is active.</summary>
public sealed record CustomerDriverView(string Name, string VehicleLabel, bool CanContact);

public sealed record CustomerDriverContactView(string PhoneNumber);

/// <summary>状态轨迹事件：实际事件时间、主状态与细分类型；不含操作者信息。</summary>
public sealed record CustomerTimelineEventView(string Type, string Status, DateTimeOffset OccurredAt);

/// <summary>积分引用（不结算）：RESERVED / REDEEMED / RETURNED / NONE。</summary>
public sealed record CustomerPointsView(long Required, string State);

/// <summary>取消信息。</summary>
public sealed record CustomerCancellationView(string ReasonCode, DateTimeOffset CancelledAt);

/// <summary>
/// 再次预订的输入：只复用地点 / 人数 / 服务 / 备注，不继承旧订单的资格、权益结论、车辆分配、审批结论或幂等键；
/// 新建订单时所有规则重新校验。仅终态订单提供。
/// </summary>
public sealed record CustomerRebookInputView(
    string? ProductCode,
    string PickupLocationId,
    string DropoffLocationId,
    string VehicleClass,
    int Passengers,
    string? SpecialRequests);

/// <summary>
/// 客户订单详情：列表字段加备注、司机车辆摘要、状态轨迹、积分引用、取消信息与再次预订输入。
/// </summary>
/// <param name="Summary">列表字段。</param>
/// <param name="SpecialRequests">订单备注；没有时省略。</param>
/// <param name="AssignmentStatus">司机分配状态：UNASSIGNED / ASSIGNED。</param>
/// <param name="Driver">司机与车辆摘要；未分配时省略。</param>
/// <param name="Timeline">状态轨迹，按发生顺序。</param>
/// <param name="Points">积分引用。</param>
/// <param name="Cancellation">取消信息；未取消时省略。</param>
/// <param name="RebookInput">再次预订输入；仅终态订单提供。</param>
public sealed record CustomerTripView(
    CustomerTripSummaryView Summary,
    string? SpecialRequests,
    string AssignmentStatus,
    CustomerDriverView? Driver,
    IReadOnlyList<CustomerTimelineEventView> Timeline,
    CustomerPointsView Points,
    CustomerCancellationView? Cancellation,
    CustomerRebookInputView? RebookInput);

/// <summary>
/// 取消能力与政策。
/// </summary>
/// <param name="TripId">订单标识。</param>
/// <param name="TripReference">订单业务单号。</param>
/// <param name="Status">后端主状态。</param>
/// <param name="CanCancel">当前是否可取消。</param>
/// <param name="BlockedReason">不可取消的原因：STATE_NOT_CANCELLABLE / WITHIN_CANCELLATION_CUTOFF；可取消时省略。</param>
/// <param name="PolicyReference">订单所属物业的取消政策引用，仅展示。</param>
/// <param name="ReasonCode">取消时使用的取消原因代码，由服务端配置映射，客户端不需提交。</param>
public sealed record CustomerCancellationPolicyView(
    string TripId,
    string TripReference,
    string Status,
    bool CanCancel,
    string? BlockedReason,
    string? PolicyReference,
    string ReasonCode);
