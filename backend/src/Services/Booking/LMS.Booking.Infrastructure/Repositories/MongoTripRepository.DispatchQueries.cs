using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Application.DispatchOrders.Models;
using LMS.Booking.Domain.Trips;
using LMS.Booking.Infrastructure.Documents;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

// 派单队列的订单查询。物业范围、渠道、时间与搜索条件对列表、总数和计数完全一致；
// 渠道缺失的存量行程按客服代订处理。
internal sealed partial class MongoTripRepository
{
    public async Task<PageResult<Trip>> ListDispatchOrdersAsync(ListDispatchOrdersQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            DispatchFilter(query.Scope, query.Filter),
            MongoSortFactory.Create<TripDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<Trip>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<DispatchOrderCounts> CountDispatchOrdersAsync(DispatchOrderCountQuery query, CancellationToken cancellationToken)
    {
        var scoped = DispatchFilter(query.Scope, query.Filter);
        var byStatus = await CountDispatchByStatusAsync(scoped, cancellationToken);
        var awaiting = await CountAssignableInAsync(scoped, query.Sets.Awaiting, cancellationToken);
        var processing = await CountAssignableInAsync(scoped, query.Sets.Processing, cancellationToken);
        var redispatch = await CountAssignableInAsync(scoped, query.Sets.Redispatch, cancellationToken);

        return new DispatchOrderCounts(
            CountOf(byStatus, DispatchOrderGroup.PendingConfirmation),
            CountOf(byStatus, DispatchOrderGroup.Confirmed),
            CountOf(byStatus, DispatchOrderGroup.Assigned),
            CountOf(byStatus, DispatchOrderGroup.Completed),
            CountOf(byStatus, DispatchOrderGroup.Cancelled),
            awaiting,
            processing,
            redispatch);
    }

    private async Task<Dictionary<TripStatus, long>> CountDispatchByStatusAsync(
        FilterDefinition<TripDocument> scoped,
        CancellationToken cancellationToken)
    {
        var grouping = await Collection
            .Aggregate()
            .Match(scoped)
            .Group(candidate => candidate.Status, grouped => new { Status = grouped.Key, Count = grouped.Count() })
            .ToListAsync(cancellationToken);

        var counts = new Dictionary<TripStatus, long>();
        foreach (var item in grouping)
        {
            if (WireEnum.TryParse<TripStatus>(item.Status, out var status))
            {
                counts[status] = item.Count;
            }
        }

        return counts;
    }

    private async Task<long> CountAssignableInAsync(
        FilterDefinition<TripDocument> scoped,
        IReadOnlyList<string> tripIds,
        CancellationToken cancellationToken)
    {
        if (tripIds.Count == 0)
        {
            return 0;
        }

        var filter = Builders<TripDocument>.Filter;
        var assignable = StatusFilter(DispatchOrderGroup.Unassigned);

        return await Collection.CountDocumentsAsync(
            filter.And(scoped, assignable, filter.In(candidate => candidate.TripId, tripIds)),
            cancellationToken: cancellationToken);
    }

    private static long CountOf(Dictionary<TripStatus, long> byStatus, DispatchOrderGroup group) =>
        DispatchOrderGroups.StatusesOf(group)!.Sum(status => byStatus.GetValueOrDefault(status));

    private static FilterDefinition<TripDocument> DispatchFilter(PropertyScope scope, DispatchOrderFilter criteria)
    {
        var filter = Builders<TripDocument>.Filter;

        return new MongoFilterBuilder<TripDocument>(VisibleTo(scope))
            .When(criteria.PropertyId is not null, () => filter.Eq(candidate => candidate.PropertyId, criteria.PropertyId))
            .When(criteria.Channel is not null, () => ChannelFilter(criteria.Channel!.Value))
            .When(criteria.From is not null, () => filter.Gte(candidate => candidate.PickupAt, criteria.From!.Value.UtcDateTime))
            .When(criteria.To is not null, () => filter.Lt(candidate => candidate.PickupAt, criteria.To!.Value.UtcDateTime))
            .When(criteria.Search.Length > 0, () => MongoTextSearch.Contains<TripDocument>(SearchFields, criteria.Search))
            .When(criteria.Group != DispatchOrderGroup.Any, () => StatusFilter(criteria.Group))
            .When(criteria.IncludeTripIds is not null, () => filter.In(candidate => candidate.TripId, criteria.IncludeTripIds))
            .When(criteria.ExcludeTripIds is { Count: > 0 }, () => filter.Nin(candidate => candidate.TripId, criteria.ExcludeTripIds))
            .Build();
    }

    private static FilterDefinition<TripDocument> StatusFilter(DispatchOrderGroup group) =>
        Builders<TripDocument>.Filter.In(
            candidate => candidate.Status,
            DispatchOrderGroups.StatusesOf(group)!.Select(WireEnum.ToWire).ToList());

    // channel 缺失的存量行程是客服代订；MongoDB 的等于 null 同时匹配缺失与显式 null。
    private static FilterDefinition<TripDocument> ChannelFilter(TripChannel channel)
    {
        var filter = Builders<TripDocument>.Filter;
        var wire = filter.Eq(candidate => candidate.Channel, WireEnum.ToWire(channel));

        return channel == TripChannel.Console
            ? filter.Or(wire, filter.Eq(candidate => candidate.Channel, null), filter.Eq(candidate => candidate.Channel, string.Empty))
            : wire;
    }
}
