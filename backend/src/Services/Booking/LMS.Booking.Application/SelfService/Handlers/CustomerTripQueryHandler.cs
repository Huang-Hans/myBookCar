using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Handlers;

// 本人订单查询直接读取权威行程，不经过推送或缓存：写入成功后的后续查询与写入结果一致。
internal sealed class CustomerTripQueryHandler(IGuestTripRepository trips) : ICustomerTripQueryHandler
{
    public async Task<OperationResult<PageResult<Trip>>> ListAsync(ListGuestTripsQuery query, CancellationToken cancellationToken) =>
        OperationResult<PageResult<Trip>>.Succeeded(await trips.ListForGuestAsync(query, cancellationToken));

    public async Task<OperationResult<Trip>> GetAsync(string tripId, string guestId, CancellationToken cancellationToken)
    {
        var trip = await trips.FindForGuestAsync(tripId, guestId, cancellationToken);

        return trip is null ? OperationResult<Trip>.Failed(OperationOutcome.NotFound) : OperationResult<Trip>.Succeeded(trip);
    }
}
