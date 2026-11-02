using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Domain.TripLocations;

// 位置共享窗口：司机已出发且行程尚未结束。窗口外不接受上报、也不向客户 / 客服返回位置。
// 经 /internal 手工推进的存量行程没有出发时间，按主状态判定已出发。
public static class TripLocationSharing
{
    // 最新位置在最后一次写入后保留的时长；行程结束后不再有写入，因此约在结束一天后自动删除。
    public static readonly TimeSpan Retention = TimeSpan.FromDays(1);

    public static bool IsSharing(Trip trip) =>
        trip.Assignment is not null && !trip.IsTerminal && HasDeparted(trip);

    private static bool HasDeparted(Trip trip) =>
        trip.Fulfillment.EnRouteAt is not null
        || trip.Status is TripStatus.DriverEnRoute or TripStatus.ArrivedAtPickup or TripStatus.InProgress;
}
