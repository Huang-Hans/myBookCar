using LMS.Booking.Domain.TripLocations;

namespace LMS.Booking.Application.TripLocations.Ports;

public interface ITripLiveLocationRepository
{
    Task<TripLiveLocation?> FindAsync(string tripId, CancellationToken cancellationToken);

    // 只有候选点的序号大于已存序号（或尚无记录）时才写入；返回写入后的当前记录，迟到的旧点不会覆盖新位置。
    Task<TripLiveLocation> SaveIfNewerAsync(TripLiveLocation candidate, CancellationToken cancellationToken);

    // 原子地占用一次推送：上次推送早于或等于 pushedBefore（或从未推送）时记为 now 并返回 true，多实例下也只有一个能成功。
    Task<bool> TryClaimPushAsync(string tripId, DateTimeOffset pushedBefore, DateTimeOffset now, CancellationToken cancellationToken);
}
