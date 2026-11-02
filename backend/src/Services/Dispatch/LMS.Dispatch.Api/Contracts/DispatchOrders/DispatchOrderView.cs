namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

/// <summary>
/// 派单页面的订单。orderStatus（订单状态）、dispatchStatus（派单状态）与 currentOffer.status（邀请状态）是三个独立口径，不能混用。
/// </summary>
/// <param name="TripId">行程标识。</param>
/// <param name="TripReference">行程业务单号。</param>
/// <param name="PropertyId">物业代码。</param>
/// <param name="Channel">CONSOLE（客服代订）或 SELF_SERVICE（客户自助）。</param>
/// <param name="Guest">宾客摘要，不含联系电话。</param>
/// <param name="Route">取送地点名称；不含地图、里程或时长。</param>
/// <param name="PickupAt">接车时间（UTC）。</param>
/// <param name="PickupMode">ASAP / TODAY / SCHEDULED。</param>
/// <param name="ServiceType">服务类型：ONE_WAY / AIRPORT_PICKUP / AIRPORT_DROPOFF / INTER_PROPERTY。</param>
/// <param name="VehicleClass">车型。</param>
/// <param name="Passengers">乘客数。</param>
/// <param name="Luggage">行李数。</param>
/// <param name="SpecialRequests">备注；没有时省略。</param>
/// <param name="OrderStatus">Booking 的订单状态（八态）。</param>
/// <param name="DispatchStatus">派单状态：PENDING_CONFIRMATION（兼容值，不产生）/ PENDING（待派单）/ AWAITING_RESPONSE / PROCESSING / DRIVER_ASSIGNED / NEEDS_REDISPATCH / COMPLETED / CANCELLED。</param>
/// <param name="CurrentOffer">最近一轮邀请；从未发送过为空。</param>
/// <param name="Assignment">Booking 的权威司机指派；尚未指派为空。</param>
/// <param name="BookedBy">下单经办人（客服用户或客户本人）；name 是下单时的姓名快照，存量订单省略。</param>
/// <param name="CreatedAt">下单时间（UTC）。</param>
/// <param name="CanDispatch">此刻是否可以发送邀请。</param>
/// <param name="BlockedReason">不可发送邀请的原因：PENDING_CONFIRMATION / OFFER_PENDING / OFFER_PROCESSING / DRIVER_ASSIGNED / ORDER_CANCELLED / ORDER_COMPLETED；可发送时省略。</param>
/// <param name="SyncStatus">IN_SYNC，或 RECOVERING（Booking 已指派而邀请仍在处理中，以 Booking 为准且不允许重复派单）。</param>
/// <param name="Version">派单版本 = 最近一轮邀请的轮次（从未发送为 0）；发送邀请时可作为 X-Expected-Version。</param>
/// <param name="SyncedAt">本次读取 Booking 与 Dispatch 的服务端时间（UTC）；订单实时读取，没有额外的同步滞后。</param>
public sealed record DispatchOrderView(
    string TripId,
    string TripReference,
    string PropertyId,
    string Channel,
    DispatchOrderGuestView Guest,
    DispatchOrderRouteView Route,
    DateTimeOffset PickupAt,
    string PickupMode,
    string ServiceType,
    string VehicleClass,
    int Passengers,
    int Luggage,
    string? SpecialRequests,
    string OrderStatus,
    string DispatchStatus,
    DispatchOfferView? CurrentOffer,
    DispatchOrderAssignmentView? Assignment,
    DispatchOrderBookedByView BookedBy,
    DateTimeOffset CreatedAt,
    bool CanDispatch,
    string? BlockedReason,
    string SyncStatus,
    long Version,
    DateTimeOffset SyncedAt);
