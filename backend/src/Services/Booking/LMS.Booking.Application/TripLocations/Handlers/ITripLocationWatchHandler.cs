using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.TripLocations.Models;

namespace LMS.Booking.Application.TripLocations.Handlers;

// 订阅行程位置前的查看权限校验与当前快照。无权查看与行程不存在不作区分，一律 NotFound。
public interface ITripLocationWatchHandler
{
    // 客户：只能查看本人的行程。
    Task<OperationResult<TripLocationSnapshot>> GetForCustomerAsync(string tripId, string guestId, CancellationToken cancellationToken);

    // 客服：行程须在其物业范围内（订单读取权限已在连接授权中检查）。
    Task<OperationResult<TripLocationSnapshot>> GetForStaffAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken);
}
