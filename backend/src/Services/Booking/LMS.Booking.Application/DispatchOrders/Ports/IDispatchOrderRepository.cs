using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.DispatchOrders.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DispatchOrders.Ports;

// 派单队列的订单读取。范围与筛选写在存储查询里，分页总数与计数使用同一筛选。
public interface IDispatchOrderRepository
{
    Task<PageResult<Trip>> ListDispatchOrdersAsync(ListDispatchOrdersQuery query, CancellationToken cancellationToken);

    Task<DispatchOrderCounts> CountDispatchOrdersAsync(DispatchOrderCountQuery query, CancellationToken cancellationToken);
}
