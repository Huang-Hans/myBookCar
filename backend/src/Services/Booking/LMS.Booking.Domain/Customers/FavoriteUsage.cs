namespace LMS.Booking.Domain.Customers;

// 收藏地点的用途分组：上车 / 下车。对外字面值 PICKUP / DROPOFF；同一地点可同时在两组，只在同一组内去重。
public enum FavoriteUsage
{
    Pickup,
    Dropoff
}
