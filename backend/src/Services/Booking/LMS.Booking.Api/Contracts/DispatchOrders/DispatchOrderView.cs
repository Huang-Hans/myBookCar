namespace LMS.Booking.Api.Contracts.DispatchOrders;

/// <summary>
/// 派单队列中的订单（服务间视图）。客户自助单与客服代订单同一结构，渠道见 channel；指派以 Booking 的权威记录为准。
/// </summary>
/// <param name="TripId">行程标识。</param>
/// <param name="TripReference">行程业务单号。</param>
/// <param name="PropertyId">物业代码。</param>
/// <param name="Channel">CONSOLE（客服代订）或 SELF_SERVICE（客户自助）；存量行程按 CONSOLE。</param>
/// <param name="Guest">宾客摘要，不含联系电话。</param>
/// <param name="Route">取送地点名称。</param>
/// <param name="PickupAt">接车时间（UTC）。</param>
/// <param name="PickupMode">ASAP / TODAY / SCHEDULED。</param>
/// <param name="ServiceType">服务类型：ONE_WAY / AIRPORT_PICKUP / AIRPORT_DROPOFF / INTER_PROPERTY。</param>
/// <param name="VehicleClass">车型。</param>
/// <param name="Passengers">乘客数。</param>
/// <param name="Luggage">行李数。</param>
/// <param name="SpecialRequests">备注；没有时省略。</param>
/// <param name="Status">订单状态（八态）。</param>
/// <param name="Assignment">权威司机指派；尚未指派时省略。</param>
/// <param name="BookedBy">下单经办人：客服代订为客服用户，客户自助为客户本人；姓名是下单时保存的快照。</param>
/// <param name="CreatedAt">下单时间（UTC）。</param>
/// <param name="Version">订单版本。</param>
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
    string Status,
    DispatchOrderAssignmentView? Assignment,
    DispatchOrderBookedByView BookedBy,
    DateTimeOffset CreatedAt,
    long Version);
