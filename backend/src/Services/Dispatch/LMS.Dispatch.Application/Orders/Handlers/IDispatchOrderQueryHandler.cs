using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Orders.Models;

namespace LMS.Dispatch.Application.Orders.Handlers;

public interface IDispatchOrderQueryHandler
{
    Task<OperationResult<PageResult<DispatchOrder>>> ListAsync(ListDispatchOrdersQuery query, CancellationToken cancellationToken);

    Task<OperationResult<DispatchOrderSummary>> SummarizeAsync(DispatchOrderSummaryQuery query, CancellationToken cancellationToken);

    // 读取详情时顺带把占用中的邀请与 Booking 的权威指派对齐（见 DispatchOfferReconciler）。
    Task<OperationResult<DispatchOrder>> GetAsync(string tripId, CommandContext context, CancellationToken cancellationToken);
}
