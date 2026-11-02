using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.Reviews.Models;

namespace LMS.Booking.Application.Reviews.Handlers;

public interface IReviewableTripsQueryHandler
{
    // 本人 COMPLETED 行程的服务端分页，附评价摘要；排序由 list 给出。
    Task<OperationResult<PageResult<ReviewableTrip>>> ListAsync(string guestId, ListQuery list, CancellationToken cancellationToken);
}
