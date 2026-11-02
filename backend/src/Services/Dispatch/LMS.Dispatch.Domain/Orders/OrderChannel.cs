namespace LMS.Dispatch.Domain.Orders;

// 订单来源渠道。CONSOLE 为客服代订，SELF_SERVICE 为客户自助；不由经办人角色推断。
public enum OrderChannel
{
    Console,
    SelfService
}
