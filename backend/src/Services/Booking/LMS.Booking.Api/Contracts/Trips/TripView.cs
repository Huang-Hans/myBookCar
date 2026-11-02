namespace LMS.Booking.Api.Contracts.Trips;

/// <summary>
/// 代订行程视图（HA-01/HA-04/HA-05 共用）。status 是八态主状态，用于筛选与业务判断；
/// stage 是要显示的细分阶段（在 IN_PROGRESS 内再分乘客上车 / 行驶中 / 到达目的地）。
/// assignment 是已指派司机的快照（指派时记录的姓名与车辆文本）；未指派时为 null。
/// </summary>
public sealed record TripView(
    string TripId,
    string TripReference,
    string PropertyId,
    TripGuestView Guest,
    TripRouteView Route,
    DateTimeOffset PickupAt,
    string PickupMode,
    string ServiceType,
    string VehicleClass,
    int Passengers,
    int Luggage,
    string? SpecialRequests,
    string Status,
    string Stage,
    TripPointsView Points,
    TripCancellationView? Cancellation,
    BookedByView BookedBy,
    string ConfirmationLanguage,
    long Version,
    TripModificationView? LastModification = null,
    TripAssignmentView? Assignment = null,
    string Channel = "CONSOLE",
    string? ProductCode = null);

/// <summary>已指派司机的快照：指派时记录的司机标识、姓名与车辆文本，不随司机主档后续修改而变化。</summary>
public sealed record TripAssignmentView(string DriverId, string DriverName, string VehicleLabel, DateTimeOffset AssignedAt);

/// <summary>下单时冻结的宾客快照；电话为掩码值。</summary>
public sealed record TripGuestView(
    string GuestId,
    string Name,
    string TierCode,
    string? RoomNumber,
    string? MemberId,
    string? PhoneMasked);

/// <summary>路线；isCrossBorder 一期仅记录（D8）。</summary>
public sealed record TripRouteView(
    string PickupLocationId,
    string PickupName,
    string DropoffLocationId,
    string DropoffName,
    bool IsCrossBorder);

/// <summary>积分引用（不结算）：RESERVED / REDEEMED / RETURNED / NONE。</summary>
public sealed record TripPointsView(long Required, string? Reference, string State);

/// <summary>取消记录；原因码来自上传清单，政策为展示引用。</summary>
public sealed record TripCancellationView(
    string ReasonCode,
    string Comments,
    string CancelledBy,
    DateTimeOffset CancelledAt,
    string? PolicyReference);

/// <summary>代订经办人（HA-03 / PM-AD-51），取自令牌解析；name 是下单时保存的姓名快照，早于该字段的存量行程省略。</summary>
public sealed record BookedByView(string UserId, string RoleCode, string? Name = null);

/// <summary>取消前政策展示（HA-10）。</summary>
public sealed record TripCancellationPolicyView(string TripReference, string PropertyId, string Status, string? PolicyReference);
