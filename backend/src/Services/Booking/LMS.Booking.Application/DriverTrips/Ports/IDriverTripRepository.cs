using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DriverTrips.Ports;

// 司机本人维度的行程读取。归属条件（assignment.driverId）写在存储查询里：他人的行程与不存在不作区分，
// 存量行程没有指派，因此不会出现在任何司机的结果里。写入仍经 ITripRepository.TryReplaceAsync（版本条件替换）。
public interface IDriverTripRepository
{
    Task<Trip?> FindByIdAsync(string tripId, ITransactionContext context, CancellationToken cancellationToken);

    Task<PageResult<Trip>> ListForDriverAsync(ListDriverTripsQuery query, CancellationToken cancellationToken);

    Task<Trip?> FindForDriverAsync(string tripId, string driverId, ITransactionContext context, CancellationToken cancellationToken);

    Task<Trip?> FindForDriverAsync(string tripId, string driverId, CancellationToken cancellationToken);

    // 司机当前有效（已指派未终态）的行程；没有为 null。
    Task<Trip?> FindActiveForDriverAsync(string driverId, CancellationToken cancellationToken);
}
