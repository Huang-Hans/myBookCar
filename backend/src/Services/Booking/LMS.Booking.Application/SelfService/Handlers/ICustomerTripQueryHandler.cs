using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Handlers;

public interface ICustomerTripQueryHandler
{
    Task<OperationResult<PageResult<Trip>>> ListAsync(ListGuestTripsQuery query, CancellationToken cancellationToken);

    // 只返回归属本人的订单；他人的订单与不存在不作区分。
    Task<OperationResult<Trip>> GetAsync(string tripId, string guestId, CancellationToken cancellationToken);
}
