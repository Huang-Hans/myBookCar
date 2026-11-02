namespace LMS.Booking.Application.DriverTrips.Ports;

// Dispatch 的司机可用状态归 Dispatch 所有；行程结束后由 Booking 通知它释放执行该行程的司机。
// 尽力而为：失败只记录日志，不重试、不补发，也不影响已经提交的行程结果，因此不抛出异常。
public interface IDispatchDriverReleases
{
    Task ReleaseAsync(string driverId, string tripId, DateTimeOffset endedAt, CancellationToken cancellationToken);
}
