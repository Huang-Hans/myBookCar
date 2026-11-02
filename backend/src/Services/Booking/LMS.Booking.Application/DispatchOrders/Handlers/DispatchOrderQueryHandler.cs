using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.DispatchOrders.Models;
using LMS.Booking.Application.DispatchOrders.Ports;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DispatchOrders.Handlers;

// 派单队列的订单读取：客户自助单与客服代订单在同一个行程集合里，因此天然在同一队列；这里只做范围与筛选。
internal sealed class DispatchOrderQueryHandler(IDispatchOrderRepository orders, ITripRepository trips) : IDispatchOrderQueryHandler
{
    public async Task<OperationResult<PageResult<Trip>>> ListAsync(ListDispatchOrdersQuery query, CancellationToken cancellationToken) =>
        IsOutOfScope(query.Scope, query.Filter)
            ? OperationResult<PageResult<Trip>>.Failed(OperationOutcome.Forbidden)
            : OperationResult<PageResult<Trip>>.Succeeded(await orders.ListDispatchOrdersAsync(query, cancellationToken));

    public async Task<OperationResult<DispatchOrderCounts>> CountAsync(DispatchOrderCountQuery query, CancellationToken cancellationToken) =>
        IsOutOfScope(query.Scope, query.Filter)
            ? OperationResult<DispatchOrderCounts>.Failed(OperationOutcome.Forbidden)
            : OperationResult<DispatchOrderCounts>.Succeeded(await orders.CountDispatchOrdersAsync(query, cancellationToken));

    public async Task<OperationResult<Trip>> GetAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var trip = await trips.FindVisibleAsync(tripId, scope, cancellationToken);

        return trip is null ? OperationResult<Trip>.Failed(OperationOutcome.NotFound) : OperationResult<Trip>.Succeeded(trip);
    }

    private static bool IsOutOfScope(PropertyScope scope, DispatchOrderFilter filter) =>
        filter.PropertyId is not null && !scope.Allows(filter.PropertyId);
}
