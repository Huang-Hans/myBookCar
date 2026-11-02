using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Application.Orders.Models;
using LMS.Dispatch.Application.Orders.Options;
using LMS.Dispatch.Application.Orders.Services;
using LMS.Dispatch.Domain.Offers;
using LMS.Dispatch.Domain.Orders;
using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Application.Orders.Handlers;

// 派单队列的读取：订单实时来自 Booking，邀请来自 Dispatch，派单状态在读取时推导。
// 来源不可用返回 503，不会被当作空队列；列表、总数与计数共用同一物业范围与筛选。
internal sealed class DispatchOrderQueryHandler(
    IBookingDispatchOrders bookings,
    IDriverOfferRepository offers,
    DispatchQueuePlanner planner,
    DispatchOrderComposer composer,
    DispatchOfferReconciler reconciler,
    IOptions<DispatchQueueOptions> options,
    TimeProvider timeProvider) : IDispatchOrderQueryHandler
{
    public async Task<OperationResult<PageResult<DispatchOrder>>> ListAsync(ListDispatchOrdersQuery query, CancellationToken cancellationToken)
    {
        var filter = query.Filter;
        if (IsOutOfScope(query.Context.Scope, filter.PropertyId))
        {
            return OperationResult<PageResult<DispatchOrder>>.Failed(OperationOutcome.Forbidden);
        }

        var plan = await planner.PlanAsync(filter.Status, OfferScope(query.Context.Scope, filter.PropertyId), cancellationToken);
        if (!plan.IsSuccess)
        {
            return OperationResult<PageResult<DispatchOrder>>.FailureOf(plan);
        }

        var page = await bookings.ListAsync(ToBookingQuery(query, plan.Value!), cancellationToken);

        return page.IsSuccess
            ? OperationResult<PageResult<DispatchOrder>>.Succeeded(await ComposeAsync(page.Value!, cancellationToken))
            : OperationResult<PageResult<DispatchOrder>>.Failed(OrderOutcomes.Of(page.Outcome));
    }

    public async Task<OperationResult<DispatchOrderSummary>> SummarizeAsync(DispatchOrderSummaryQuery query, CancellationToken cancellationToken)
    {
        var filter = query.Filter;
        if (IsOutOfScope(query.Context.Scope, filter.PropertyId))
        {
            return OperationResult<DispatchOrderSummary>.Failed(OperationOutcome.Forbidden);
        }

        var index = await planner.LoadIndexAsync(OfferScope(query.Context.Scope, filter.PropertyId), cancellationToken);
        if (index is null)
        {
            return OperationResult<DispatchOrderSummary>.Failed(OperationOutcome.SourceUnavailable);
        }

        var counts = await bookings.CountAsync(ToCountQuery(query, index), cancellationToken);

        return counts.IsSuccess
            ? OperationResult<DispatchOrderSummary>.Succeeded(Summarize(counts.Value!))
            : OperationResult<DispatchOrderSummary>.Failed(OrderOutcomes.Of(counts.Outcome));
    }

    public async Task<OperationResult<DispatchOrder>> GetAsync(string tripId, CommandContext context, CancellationToken cancellationToken)
    {
        var order = await bookings.GetAsync(tripId, cancellationToken);
        if (!order.IsSuccess)
        {
            return OperationResult<DispatchOrder>.Failed(OrderOutcomes.Of(order.Outcome));
        }

        var latest = (await LatestOffersAsync([tripId], cancellationToken)).GetValueOrDefault(tripId);
        var reconciled = await reconciler.ReconcileAsync(order.Value!, latest, context, cancellationToken);

        return OperationResult<DispatchOrder>.Succeeded(composer.Compose(order.Value!, reconciled));
    }

    private async Task<PageResult<DispatchOrder>> ComposeAsync(PageResult<BookingOrder> page, CancellationToken cancellationToken)
    {
        var latest = await LatestOffersAsync(page.Items.Select(order => order.TripId).ToList(), cancellationToken);
        var items = page.Items.Select(order => composer.Compose(order, latest.GetValueOrDefault(order.TripId))).ToList();

        return new PageResult<DispatchOrder>(items, page.TotalItems);
    }

    private async Task<Dictionary<string, DriverOffer>> LatestOffersAsync(IReadOnlyList<string> tripIds, CancellationToken cancellationToken)
    {
        var since = timeProvider.GetUtcNow() - options.Value.OfferLookback;
        var latest = await offers.FindLatestForTripsAsync(tripIds, since, cancellationToken);

        return latest.ToDictionary(offer => offer.TripId);
    }

    private static bool IsOutOfScope(PropertyScope scope, string? propertyId) => propertyId is not null && !scope.Allows(propertyId);

    // 邀请只在所选物业内统计；未选择物业时使用调用者的授权范围。
    private static PropertyScope OfferScope(PropertyScope scope, string? propertyId) =>
        propertyId is null ? scope : PropertyScope.ForProperty(propertyId);

    private static BookingOrderQuery ToBookingQuery(ListDispatchOrdersQuery query, QueueFilterPlan plan) => new(
        query.List, query.Filter.PropertyId, query.Filter.Channel, query.Filter.From, query.Filter.To,
        plan.Group, plan.Include, plan.Exclude);

    private static BookingOrderCountQuery ToCountQuery(DispatchOrderSummaryQuery query, OfferStateIndex index) => new(
        query.Search, query.Filter.PropertyId, query.Filter.Channel, query.Filter.From, query.Filter.To,
        new BookingOrderTripSets(index.Awaiting, index.Processing, index.Redispatch));

    private static DispatchOrderSummary Summarize(BookingOrderCounts counts)
    {
        var pending = Math.Max(0, counts.PendingConfirmation + counts.Confirmed - counts.ConfirmedAwaiting - counts.ConfirmedProcessing - counts.ConfirmedRedispatch);
        var byStatus = new Dictionary<DispatchStatus, long>
        {
            [DispatchStatus.PendingConfirmation] = 0,
            [DispatchStatus.Pending] = pending,
            [DispatchStatus.AwaitingResponse] = counts.ConfirmedAwaiting,
            [DispatchStatus.Processing] = counts.ConfirmedProcessing,
            [DispatchStatus.DriverAssigned] = counts.Assigned,
            [DispatchStatus.NeedsRedispatch] = counts.ConfirmedRedispatch,
            [DispatchStatus.Completed] = counts.Completed,
            [DispatchStatus.Cancelled] = counts.Cancelled
        };

        return new DispatchOrderSummary(byStatus.Values.Sum(), byStatus);
    }
}
