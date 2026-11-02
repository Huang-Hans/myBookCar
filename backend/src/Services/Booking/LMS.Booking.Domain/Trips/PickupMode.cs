namespace LMS.Booking.Domain.Trips;

// 取车时间模式。ASAP 由服务端按固定缓冲计算 pickupAt，不受最短提前时间限制。
public enum PickupMode
{
    Asap,
    Today,
    Scheduled
}
