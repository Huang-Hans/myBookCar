using LMS.Booking.Domain.TripLocations;

namespace LMS.Booking.Application.TripLocations.Ports;

public interface ITripLocationTrackRepository
{
    // 追加一批轨迹点；同一行程同一序号只保存一次，重传的点被忽略，其他写入失败直接抛出。
    Task AppendAsync(string tripId, IReadOnlyList<LocationPoint> points, DateTimeOffset receivedAt, DateTimeOffset expiresAt, CancellationToken cancellationToken);

    // 按序号升序返回序号大于 afterSeq 的点。
    Task<IReadOnlyList<LocationPoint>> ListAsync(string tripId, long afterSeq, CancellationToken cancellationToken);

    // 一次读取多个行程的全部轨迹点，按行程分组、组内按序号升序；没有轨迹的行程不出现在结果中。
    Task<IReadOnlyDictionary<string, IReadOnlyList<LocationPoint>>> ListForTripsAsync(IReadOnlyCollection<string> tripIds, CancellationToken cancellationToken);
}
