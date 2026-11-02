using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Orders.Handlers;

public interface IDispatchCandidateQueryHandler
{
    // 订单当前可以派单时，返回同物业、启用、AVAILABLE 且未被邀请占用的司机；订单不可派单为状态冲突。
    Task<OperationResult<PageResult<Driver>>> ListAsync(string tripId, ListQuery list, CancellationToken cancellationToken);
}
