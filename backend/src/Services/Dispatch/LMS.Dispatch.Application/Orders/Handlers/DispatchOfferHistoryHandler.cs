using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Application.Orders.Services;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Orders.Handlers;

internal sealed class DispatchOfferHistoryHandler(IBookingDispatchOrders bookings, IDriverOfferRepository offers) : IDispatchOfferHistoryHandler
{
    public async Task<OperationResult<PageResult<DriverOffer>>> ListAsync(string tripId, ListQuery list, CancellationToken cancellationToken)
    {
        var order = await bookings.GetAsync(tripId, cancellationToken);

        return order.IsSuccess
            ? OperationResult<PageResult<DriverOffer>>.Succeeded(await offers.ListForTripAsync(tripId, list, cancellationToken))
            : OperationResult<PageResult<DriverOffer>>.Failed(OrderOutcomes.Of(order.Outcome));
    }
}
