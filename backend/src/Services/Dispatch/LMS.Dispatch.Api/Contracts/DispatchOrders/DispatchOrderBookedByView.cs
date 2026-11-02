namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

/// <summary>下单经办人。name 是下单时的姓名快照，存量订单省略，不用当前登录人替代。</summary>
public sealed record DispatchOrderBookedByView(string UserId, string RoleCode, string? Name);
