namespace LMS.Booking.Domain.Trips;

// 行程来源渠道。CONSOLE 为客服代订；SELF_SERVICE 为客户自助预订（经办人是客户本人）。
// 存量行程没有该字段，读取时按 CONSOLE 处理。
public enum TripChannel
{
    Console,
    SelfService
}
