using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DriverTrips.Handlers;

public interface IDriverTripQueryHandler
{
    Task<OperationResult<PageResult<Trip>>> ListAsync(ListDriverTripsQuery query, CancellationToken cancellationToken);

    // 只返回归属本人的行程；他人的行程与不存在不作区分。
    Task<OperationResult<Trip>> GetAsync(string tripId, string driverId, CancellationToken cancellationToken);

    // 本人当前有效（已指派未终态）的行程；没有为 NotFound，查询失败不会被掩盖成没有。
    Task<OperationResult<Trip>> GetActiveAsync(string driverId, CancellationToken cancellationToken);

    // 调度来源读取待分配行程（员工权限，按物业范围限定）。
    Task<OperationResult<Trip>> GetForAssignmentAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken);
}
