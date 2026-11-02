namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

/// <summary>
/// 当前筛选条件（不含派单状态）下各派单状态的订单数。七个状态恒全量返回；总数是它们之和。
/// </summary>
/// <param name="Total">订单总数。</param>
/// <param name="Counts">键为派单状态字面值；待派单计入 PENDING（含存量 PENDING 订单），兼容键 PENDING_CONFIRMATION 恒为 0。</param>
public sealed record DispatchOrderSummaryView(long Total, IReadOnlyDictionary<string, long> Counts);
