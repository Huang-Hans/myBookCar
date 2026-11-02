using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Application.Orders.Models;
using LMS.Dispatch.Application.Orders.Options;
using LMS.Dispatch.Domain.Orders;
using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Application.Orders.Services;

// 派单状态筛选的执行计划：订单状态由 Booking 筛选，邀请状态由 Dispatch 算出订单集合后传给 Booking 限定，
// 因此列表、总数与各状态计数使用同一口径，且不依赖前端首屏的若干条数据。
internal sealed class DispatchQueuePlanner(
    IDriverOfferRepository offers,
    IOptions<DispatchQueueOptions> options,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<QueueFilterPlan>> PlanAsync(DispatchStatus? status, PropertyScope offerScope, CancellationToken cancellationToken)
    {
        if (status is null)
        {
            return OperationResult<QueueFilterPlan>.Succeeded(new QueueFilterPlan(BookingOrderGroup.Any, null, null));
        }

        if (GroupOf(status.Value) is { } group)
        {
            return OperationResult<QueueFilterPlan>.Succeeded(new QueueFilterPlan(group, status == DispatchStatus.PendingConfirmation ? [] : null, null));
        }

        var index = await LoadIndexAsync(offerScope, cancellationToken);

        return index is null
            ? OperationResult<QueueFilterPlan>.Failed(OperationOutcome.SourceUnavailable)
            : OperationResult<QueueFilterPlan>.Succeeded(UnassignedPlan(status.Value, index));
    }

    // 范围内最近一轮邀请的分类；集合超过上限时返回 null，调用方按来源不可用处理，而不是静默截断。
    public async Task<OfferStateIndex?> LoadIndexAsync(PropertyScope offerScope, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var states = await offers.ListLatestStatesAsync(offerScope, now - options.Value.OfferLookback, cancellationToken);
        var index = OfferStateIndex.Build(states, now);

        return index.Size > options.Value.MaxStatusSetSize ? null : index;
    }

    private static BookingOrderGroup? GroupOf(DispatchStatus status) => status switch
    {
        DispatchStatus.PendingConfirmation => BookingOrderGroup.PendingConfirmation,
        DispatchStatus.DriverAssigned => BookingOrderGroup.Assigned,
        DispatchStatus.Completed => BookingOrderGroup.Completed,
        DispatchStatus.Cancelled => BookingOrderGroup.Cancelled,
        _ => null
    };

    private static QueueFilterPlan UnassignedPlan(DispatchStatus status, OfferStateIndex index) => status switch
    {
        DispatchStatus.AwaitingResponse => new QueueFilterPlan(BookingOrderGroup.Unassigned, index.Awaiting, null),
        DispatchStatus.Processing => new QueueFilterPlan(BookingOrderGroup.Unassigned, index.Processing, null),
        DispatchStatus.NeedsRedispatch => new QueueFilterPlan(BookingOrderGroup.Unassigned, index.Redispatch, null),
        DispatchStatus.Pending => new QueueFilterPlan(BookingOrderGroup.Unassigned, null, index.All),
        _ => new QueueFilterPlan(BookingOrderGroup.Unassigned, null, index.All)
    };
}
