using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Orders.Handlers;

public interface IDispatchOfferHistoryHandler
{
    // 订单对调用者不可见时为不存在；否则返回该订单全部邀请轮次。
    Task<OperationResult<PageResult<DriverOffer>>> ListAsync(string tripId, ListQuery list, CancellationToken cancellationToken);
}
