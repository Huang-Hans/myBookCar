namespace LMS.Booking.Application.Trips.Models;

// 司机行程视图：ACTIVE 为当前有效（已指派未终态）的行程；HISTORY 为已完成或已取消且归属本人的行程。
public enum DriverTripView
{
    Active,
    History
}
