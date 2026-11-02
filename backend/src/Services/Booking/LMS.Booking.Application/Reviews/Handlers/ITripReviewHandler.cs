using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Reviews.Commands;
using LMS.Booking.Domain.Reviews;

namespace LMS.Booking.Application.Reviews.Handlers;

public interface ITripReviewHandler
{
    // 本人行程的评价；行程不是本人的、不存在或还没有评价都是 NotFound。
    Task<OperationResult<TripReview>> GetAsync(string tripId, string guestId, CancellationToken cancellationToken);

    Task<OperationResult<TripReview>> UpsertAsync(UpsertTripReviewCommand command, CancellationToken cancellationToken);
}
