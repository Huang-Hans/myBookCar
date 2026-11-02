using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Orders.Models;
using LMS.Dispatch.Domain.Offers;
using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Application.Orders.Services;

// 把 Booking 的订单与 Dispatch 最近一轮邀请合成派单页面的订单。派单状态每次读取时推导，不单独存储。
internal sealed class DispatchOrderComposer(TimeProvider timeProvider)
{
    public DispatchOrder Compose(BookingOrder order, DriverOffer? latestOffer)
    {
        var now = timeProvider.GetUtcNow();
        var status = DispatchStatusRules.Resolve(order.Status, latestOffer, now);

        return new DispatchOrder(
            order,
            status,
            latestOffer,
            order.IsAssignable && DispatchStatusRules.CanDispatch(status),
            DispatchStatusRules.BlockedReasonOf(status),
            DispatchStatusRules.SyncOf(status, latestOffer),
            latestOffer?.Round ?? 0,
            now);
    }
}
