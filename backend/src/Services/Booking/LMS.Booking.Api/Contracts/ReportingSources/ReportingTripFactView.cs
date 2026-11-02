namespace LMS.Booking.Api.Contracts.ReportingSources;

/// <summary>
/// 报表来源的一条行程事实。只含报表需要的字段，不含电话、房号、备注等。
/// </summary>
/// <param name="TripId">行程标识。</param>
/// <param name="TripReference">行程编号。</param>
/// <param name="PropertyId">物业代码。</param>
/// <param name="GuestId">宾客标识，用于判断取消是否由客户本人发起。</param>
/// <param name="GuestName">宾客姓名快照。</param>
/// <param name="PickupAt">计划接车时间（UTC）。</param>
/// <param name="Status">行程状态。</param>
/// <param name="IsCrossBorder">是否跨境路线。</param>
/// <param name="Product">客户自助预订的服务产品；客服代订为 null。</param>
/// <param name="Points">积分引用。</param>
/// <param name="Assignment">司机指派快照；未指派为 null。</param>
/// <param name="EnRouteAt">司机出发时间（UTC）；未发生为 null。</param>
/// <param name="PickedUpAt">实际接到客人时间（UTC）；未发生为 null。</param>
/// <param name="CompletedAt">完成时间（UTC）；未发生为 null。</param>
/// <param name="Cancellation">取消记录；未取消为 null。</param>
/// <param name="PickupToCompleteMeters">接到客人到完成的轨迹距离（米）；轨迹不足时为 null。</param>
/// <param name="EnRouteToCompleteMeters">出发到完成的轨迹距离（米）；轨迹不足时为 null。</param>
public sealed record ReportingTripFactView(
    string TripId,
    string TripReference,
    string PropertyId,
    string GuestId,
    string GuestName,
    DateTime PickupAt,
    string Status,
    bool IsCrossBorder,
    string? Product,
    ReportingTripPointsView Points,
    ReportingTripAssignmentView? Assignment,
    DateTime? EnRouteAt,
    DateTime? PickedUpAt,
    DateTime? CompletedAt,
    ReportingTripCancellationView? Cancellation,
    double? PickupToCompleteMeters,
    double? EnRouteToCompleteMeters);
