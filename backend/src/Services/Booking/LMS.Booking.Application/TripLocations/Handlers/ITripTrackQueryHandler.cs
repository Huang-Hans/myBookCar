using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.TripLocations.Models;

namespace LMS.Booking.Application.TripLocations.Handlers;

// 客服查询行程行驶轨迹。行程须在其物业范围内，无权查看与行程不存在不作区分，一律 NotFound。
public interface ITripTrackQueryHandler
{
    Task<OperationResult<TripTrack>> GetForStaffAsync(string tripId, long afterSeq, PropertyScope scope, CancellationToken cancellationToken);
}
