using LMS.BuildingBlocks.Core.Querying;

namespace LMS.Dispatch.Application.Directory;

// Booking 拥有订单与权威指派；派单队列实时通过它读取（转发调度员自己的令牌），不在 Dispatch 复制订单，因此不存在同步滞后。
public interface IBookingDispatchOrders
{
    Task<BookingResult<PageResult<BookingOrder>>> ListAsync(BookingOrderQuery query, CancellationToken cancellationToken);

    Task<BookingResult<BookingOrderCounts>> CountAsync(BookingOrderCountQuery query, CancellationToken cancellationToken);

    Task<BookingResult<BookingOrder>> GetAsync(string tripId, CancellationToken cancellationToken);
}
