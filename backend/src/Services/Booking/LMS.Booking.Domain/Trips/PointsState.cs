namespace LMS.Booking.Domain.Trips;

// 积分引用状态：LMS 只记录引用，不执行扣减或退回（财务边界，BRD 1.2）。
public enum PointsState
{
    Reserved,
    Redeemed,
    Returned,
    None
}
