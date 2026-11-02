namespace LMS.Booking.Domain.Trips;

// 一次履约动作应用后的结果：主状态、事件类型与更新后的履约时间。
public sealed record FulfillmentStep(TripStatus Status, TripEventType Type, TripFulfillment Fulfillment);
