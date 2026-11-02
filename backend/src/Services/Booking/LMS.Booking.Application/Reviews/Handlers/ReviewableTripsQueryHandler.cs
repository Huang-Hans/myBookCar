using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.Reviews.Models;
using LMS.Booking.Application.Reviews.Ports;
using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Application.Trips.Models;

namespace LMS.Booking.Application.Reviews.Handlers;

// 可评价行程 = 本人（受益宾客）的 COMPLETED 行程，复用客户订单的本人查询与服务端分页；再按页取评价，不把全部行程一次加载。
internal sealed class ReviewableTripsQueryHandler(IGuestTripRepository trips, ITripReviewRepository reviews) : IReviewableTripsQueryHandler
{
    public async Task<OperationResult<PageResult<ReviewableTrip>>> ListAsync(string guestId, ListQuery list, CancellationToken cancellationToken)
    {
        var page = await trips.ListForGuestAsync(new ListGuestTripsQuery(list, guestId, GuestTripGroup.Completed), cancellationToken);
        var found = await reviews.FindForGuestAsync(guestId, page.Items.Select(trip => trip.TripId).ToList(), cancellationToken);
        var byTrip = found.ToDictionary(review => review.TripId, StringComparer.Ordinal);
        var items = page.Items.Select(trip => new ReviewableTrip(trip, byTrip.GetValueOrDefault(trip.TripId))).ToList();

        return OperationResult<PageResult<ReviewableTrip>>.Succeeded(new PageResult<ReviewableTrip>(items, page.TotalItems));
    }
}
