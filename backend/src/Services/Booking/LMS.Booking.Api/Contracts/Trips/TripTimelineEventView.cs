namespace LMS.Booking.Api.Contracts.Trips;

/// <summary>
/// 行程时间线上的一个真实事件，按发生时间升序。只列出实际记录过的事实：存量行程没有的事件不会被回填。
/// </summary>
/// <param name="EventId">事件标识；创建与修改事件由行程标识和序号合成。</param>
/// <param name="Type">CREATED / MODIFIED / DRIVER_ASSIGNED / STARTED_EN_ROUTE / ARRIVED_AT_PICKUP / PASSENGER_BOARDED / IN_TRANSIT / ARRIVED_AT_DESTINATION / COMPLETED / CANCELLED。</param>
/// <param name="OccurredAt">发生时间（UTC，服务端记录）。</param>
/// <param name="OrderStatus">事件发生后的订单状态（八态）；创建与修改事件省略。</param>
/// <param name="Stage">事件发生后要显示的细分阶段，乘客上车 / 行驶中 / 到达目的地各不相同；创建与修改事件省略，按 type 显示。</param>
/// <param name="ActorId">操作者标识（客服用户或司机）。</param>
/// <param name="ActorName">操作者姓名快照；没有记录时省略。</param>
/// <param name="Reason">仅修改事件：修改原因。</param>
/// <param name="ChangedFields">仅修改事件：实际改动的字段名。</param>
public sealed record TripTimelineEventView(
    string EventId,
    string Type,
    DateTimeOffset OccurredAt,
    string? OrderStatus,
    string? Stage,
    string ActorId,
    string? ActorName,
    string? Reason,
    IReadOnlyList<string>? ChangedFields);
