namespace LMS.Dispatch.Application.Directory;

// 下单经办人：客服代订为客服用户，客户自助为客户本人；Name 是下单时保存的快照，存量订单为空。
public sealed record BookingOrderBookedBy(string UserId, string RoleCode, string? Name);
