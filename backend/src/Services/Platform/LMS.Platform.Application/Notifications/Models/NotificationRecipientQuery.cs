namespace LMS.Platform.Application.Notifications.Models;

// 收件人查询：事件类型（当前仅 BOOKING_CREATED）与订单物业。
public sealed record NotificationRecipientQuery(string EventType, string PropertyId)
{
    public const string BookingCreated = "BOOKING_CREATED";
}
