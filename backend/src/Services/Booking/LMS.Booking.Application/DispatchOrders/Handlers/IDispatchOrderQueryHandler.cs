using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.DispatchOrders.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DispatchOrders.Handlers;

public interface IDispatchOrderQueryHandler
{
    Task<OperationResult<PageResult<Trip>>> ListAsync(ListDispatchOrdersQuery query, CancellationToken cancellationToken);

    Task<OperationResult<DispatchOrderCounts>> CountAsync(DispatchOrderCountQuery query, CancellationToken cancellationToken);

    Task<OperationResult<Trip>> GetAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken);
}
