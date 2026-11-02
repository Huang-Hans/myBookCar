namespace LMS.Booking.Application.Trips.Models;

// 客户订单分组。ACTIVE 是全部非终态（待确认、已确认、已指派、前往中、到达、进行中），不只等于后端 IN_PROGRESS。
public enum GuestTripGroup
{
    Active,
    Completed,
    Cancelled
}
