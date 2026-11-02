using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Application.Orders.Models;

// 派单队列筛选。Status 只用于列表；计数忽略它，以便各状态标签同时给出数量。
public sealed record DispatchOrderFilter(
    string? PropertyId,
    OrderChannel? Channel,
    DateTimeOffset? From,
    DateTimeOffset? To,
    DispatchStatus? Status);
