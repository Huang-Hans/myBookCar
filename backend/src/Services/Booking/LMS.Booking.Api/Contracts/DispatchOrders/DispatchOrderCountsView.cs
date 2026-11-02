namespace LMS.Booking.Api.Contracts.DispatchOrders;

/// <summary>同一筛选范围内的订单计数。</summary>
/// <param name="PendingConfirmation">候补 / 待确认的订单数。</param>
/// <param name="Confirmed">已确认且未指派的订单数。</param>
/// <param name="Assigned">已指派到进行中的订单数。</param>
/// <param name="Completed">已完成的订单数。</param>
/// <param name="Cancelled">已取消的订单数。</param>
/// <param name="ConfirmedAwaiting">候补 / 审批中或已确认订单中落在 awaitingTripIds 内的数量，沿用既有字段名。</param>
/// <param name="ConfirmedProcessing">候补 / 审批中或已确认订单中落在 processingTripIds 内的数量，沿用既有字段名。</param>
/// <param name="ConfirmedRedispatch">候补 / 审批中或已确认订单中落在 redispatchTripIds 内的数量，沿用既有字段名。</param>
public sealed record DispatchOrderCountsView(
    long PendingConfirmation,
    long Confirmed,
    long Assigned,
    long Completed,
    long Cancelled,
    long ConfirmedAwaiting,
    long ConfirmedProcessing,
    long ConfirmedRedispatch);
