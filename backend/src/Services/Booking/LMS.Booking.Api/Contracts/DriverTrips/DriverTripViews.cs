using LMS.Booking.Api.Contracts.Trips;

namespace LMS.Booking.Api.Contracts.DriverTrips;

/// <summary>
/// 司机行程列表项（当前行程与历史共用）。未发生的时间为空并省略，不回填计划接车时间。
/// </summary>
/// <param name="TripId">行程标识。</param>
/// <param name="TripReference">行程业务单号。</param>
/// <param name="ServiceType">服务类别：ONE_WAY / AIRPORT_PICKUP / AIRPORT_DROPOFF / INTER_PROPERTY。</param>
/// <param name="Status">主状态（八态）：DRIVER_ASSIGNED / DRIVER_EN_ROUTE / ARRIVED_AT_PICKUP / IN_PROGRESS / COMPLETED / CANCELLED。</param>
/// <param name="Stage">细分阶段：主状态加 IN_PROGRESS 内的 PASSENGER_ON_BOARD / IN_TRANSIT / ARRIVED_AT_DESTINATION。</param>
/// <param name="VehicleClass">车型要求。</param>
/// <param name="Passengers">乘客人数。</param>
/// <param name="Route">起终点（目录 ID 与名称快照）。</param>
/// <param name="PickupAt">计划接车时间（UTC）。</param>
/// <param name="AcceptedAt">接单（指派）时间（UTC）。</param>
/// <param name="PickedUpAt">实际接客时间（UTC）。</param>
/// <param name="DroppedOffAt">实际送达时间（UTC）：到达目的地的时间。</param>
/// <param name="CompletedAt">完成时间（UTC）：司机确认完成的时间。</param>
/// <param name="ClosedAt">进入终态的时间（UTC）：完成或取消。</param>
/// <param name="Version">行程版本号（只读展示，履约动作不需要提交版本）。</param>
public sealed record DriverTripSummaryView(
    string TripId,
    string TripReference,
    string ServiceType,
    string Status,
    string Stage,
    string VehicleClass,
    int Passengers,
    TripRouteView Route,
    DateTimeOffset PickupAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? PickedUpAt,
    DateTimeOffset? DroppedOffAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? ClosedAt,
    long Version);

/// <summary>乘客摘要：姓名与脱敏电话；没有真实授权联系渠道时 contact.canContactPassenger 为 false，不从脱敏号码生成拨号资料。</summary>
public sealed record DriverPassengerView(string Name, string? PhoneMasked);

/// <summary>司机车辆指派摘要。</summary>
public sealed record DriverAssignmentView(string OfferId, string VehicleLabel);

/// <summary>联系能力：真实联络渠道（Q07）尚未接入前恒为 false。</summary>
public sealed record DriverContactView(bool CanContactPassenger);

/// <summary>履约实际时间，全部为服务端提交时间（UTC）；未发生为空。</summary>
public sealed record DriverFulfillmentView(
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? EnRouteAt,
    DateTimeOffset? ArrivedAtPickupAt,
    DateTimeOffset? PickedUpAt,
    DateTimeOffset? InTransitAt,
    DateTimeOffset? ArrivedAtDestinationAt,
    DateTimeOffset? CompletedAt);

/// <summary>履约轨迹事件。</summary>
public sealed record DriverTripEventView(string EventId, string Type, string Status, DateTimeOffset OccurredAt);

/// <summary>取消信息（已取消的行程）。</summary>
public sealed record DriverTripCancellationView(string ReasonCode, DateTimeOffset CancelledAt);

/// <summary>
/// 司机行程详情：列表字段加乘客摘要、特殊说明、履约时间、轨迹与服务端给出的允许动作。接单 / 接客 / 送达 / 完成时间见 fulfillment。
/// </summary>
/// <param name="TripId">行程标识。</param>
/// <param name="TripReference">行程业务单号。</param>
/// <param name="ServiceType">服务类别。</param>
/// <param name="Status">主状态（八态）。</param>
/// <param name="Stage">细分阶段。</param>
/// <param name="VehicleClass">车型要求。</param>
/// <param name="Passengers">乘客人数。</param>
/// <param name="Route">起终点（目录 ID 与名称快照）。</param>
/// <param name="PickupAt">计划接车时间（UTC）。</param>
/// <param name="ClosedAt">进入终态的时间（UTC）；未终态时省略。</param>
/// <param name="Passenger">乘客摘要。</param>
/// <param name="SpecialRequests">乘客特殊说明与可见的客服备注；没有时省略。</param>
/// <param name="Assignment">指派摘要。</param>
/// <param name="Fulfillment">履约实际时间。</param>
/// <param name="Events">履约轨迹，按发生顺序。</param>
/// <param name="AllowedActions">当前可提交的履约动作，由服务端状态机决定；终态为空数组。</param>
/// <param name="Contact">联系能力。</param>
/// <param name="Cancellation">取消信息；未取消时省略。</param>
/// <param name="Version">行程版本号。</param>
public sealed record DriverTripDetailView(
    string TripId,
    string TripReference,
    string ServiceType,
    string Status,
    string Stage,
    string VehicleClass,
    int Passengers,
    TripRouteView Route,
    DateTimeOffset PickupAt,
    DateTimeOffset? ClosedAt,
    DriverPassengerView Passenger,
    string? SpecialRequests,
    DriverAssignmentView Assignment,
    DriverFulfillmentView Fulfillment,
    IReadOnlyList<DriverTripEventView> Events,
    IReadOnlyList<string> AllowedActions,
    DriverContactView Contact,
    DriverTripCancellationView? Cancellation,
    long Version);
