using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Application.Orders.Models;

// 当前筛选条件（不含派单状态）下各派单状态的订单数；七个状态恒全量返回，Total 是它们之和。
public sealed record DispatchOrderSummary(long Total, IReadOnlyDictionary<DispatchStatus, long> Counts);
