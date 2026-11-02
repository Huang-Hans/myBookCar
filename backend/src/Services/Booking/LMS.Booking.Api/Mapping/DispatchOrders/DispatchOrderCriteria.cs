using LMS.Booking.Application.DispatchOrders.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Api.Mapping.DispatchOrders;

// 从查询串读出的队列筛选条件（不含搜索与订单集合）。
internal sealed record DispatchOrderCriteria(
    string? PropertyId,
    TripChannel? Channel,
    DateTimeOffset? From,
    DateTimeOffset? To,
    DispatchOrderGroup Group);
