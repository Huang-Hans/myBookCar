using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.DispatchOrders.Ports;
using LMS.Booking.Application.DriverTrips.Ports;
using LMS.Booking.Application.ReportingSources.Ports;
using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.Trips;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed partial class MongoTripRepository(IMongoDatabase database, TimeProvider timeProvider)
    : ITripRepository, IDriverTripRepository, IGuestTripRepository, IDispatchOrderRepository, ITripSummaryRepository, IReportingTripRepository
{
    private static readonly string[] SearchFields =
        ["tripReference", "guest.name", "guest.roomNumber", "guest.memberId", "route.pickupName", "route.dropoffName", "propertyId", "status"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["tripId"] = "_id",
        ["tripReference"] = "tripReference",
        ["propertyId"] = "propertyId",
        ["pickupAt"] = "pickupAt",
        ["status"] = "status",
        ["serviceType"] = "serviceType",
        ["vehicleClass"] = "vehicleClass",
        ["createdAt"] = "createdAt",
        ["closedAt"] = "closedAt",
        ["guest.name"] = "guest.name",
        ["guest.roomNumber"] = "guest.roomNumber",
        ["guest.memberId"] = "guest.memberId"
    };

    private IMongoCollection<TripDocument> Collection =>
        database.GetCollection<TripDocument>(BookingCollections.Trips);

    public async Task<Trip?> FindByIdAsync(string tripId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.TripId == tripId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Trip?> FindByIdAsync(string tripId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, candidate => candidate.TripId == tripId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Trip?> FindVisibleAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(ByIdVisibleTo(tripId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Trip?> FindVisibleAsync(
        string tripId,
        PropertyScope scope,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, ByIdVisibleTo(tripId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PageResult<Trip>> ListAsync(ListTripsQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection, ListFilter(query), MongoSortFactory.Create<TripDocument>(query.List.Sort, SortFields), query.List, cancellationToken);

        return new PageResult<Trip>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    private static FilterDefinition<TripDocument> ListFilter(ListTripsQuery query) =>
        new MongoFilterBuilder<TripDocument>(TripFilter(query.Scope, query.PropertyId, query.From, query.To, query.RedemptionState, query.List.Search))
            .When(query.Channel is not null, () => ChannelFilter(query.Channel!.Value))
            .When(query.Status is not null, () => Builders<TripDocument>.Filter.Eq(candidate => candidate.Status, WireEnum.ToWire(query.Status!.Value)))
            .Build();

    public async Task<IReadOnlyList<Trip>> ListRecentByGuestAsync(
        string guestId,
        PropertyScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        var filter = Builders<TripDocument>.Filter.And(
            Builders<TripDocument>.Filter.Eq("guest.guestId", guestId),
            VisibleTo(scope));
        var documents = await Collection.Find(filter)
            .SortByDescending(candidate => candidate.PickupAt)
            .Limit(take)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public Task InsertAsync(Trip trip, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), TripDocument.FromDomain(trip), cancellationToken);

    public Task<bool> TryReplaceAsync(
        Trip updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.TripId,
            expectedVersion,
            TripDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);

    // 当日序号 = 当日已有单数 + 1。并发撞号由 ux_trip_reference 兜底（创建工作流按重复处理）。
    public async Task<string> NextReferenceAsync(DateOnly macauDate, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        // 前缀仅含字母数字与连字符，可直接作为正则字面量。
        var prefix = $"{Trip.ReferencePrefix}{macauDate:yyyyMMdd}-";
        var filter = Builders<TripDocument>.Filter.Regex(
            candidate => candidate.TripReference, new MongoDB.Bson.BsonRegularExpression($"^{prefix}"));
        var createdToday = await Collection.CountDocumentsAsync(session, filter, cancellationToken: cancellationToken);

        return $"{prefix}{createdToday + 1}";
    }

    public async Task<long> CountDailyActiveAsync(string propertyId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var filter = Builders<TripDocument>.Filter.And(
            Builders<TripDocument>.Filter.Eq(candidate => candidate.PropertyId, propertyId),
            Builders<TripDocument>.Filter.Gte(candidate => candidate.PickupAt, fromUtc.UtcDateTime),
            Builders<TripDocument>.Filter.Lt(candidate => candidate.PickupAt, toUtc.UtcDateTime),
            Builders<TripDocument>.Filter.Ne(candidate => candidate.Status, WireEnum.ToWire(TripStatus.Cancelled)));

        return await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyDictionary<TripStatus, long>> CountByStatusAsync(PropertyScope scope, CancellationToken cancellationToken) =>
        CountByStatusAsync(VisibleTo(scope), cancellationToken);

    public Task<IReadOnlyDictionary<TripStatus, long>> CountByStatusAsync(TripSummaryQuery query, CancellationToken cancellationToken) =>
        CountByStatusAsync(
            TripFilter(query.Scope, query.PropertyId, query.From, query.To, query.RedemptionState, query.Search),
            cancellationToken);

    private async Task<IReadOnlyDictionary<TripStatus, long>> CountByStatusAsync(
        FilterDefinition<TripDocument> filter,
        CancellationToken cancellationToken)
    {
        var grouping = await Collection
            .Aggregate()
            .Match(filter)
            .Group(candidate => candidate.Status, grouped => new { Status = grouped.Key, Count = grouped.Count() })
            .ToListAsync(cancellationToken);

        var counts = Enum.GetValues<TripStatus>().ToDictionary(status => status, _ => 0L);
        foreach (var item in grouping)
        {
            if (WireEnum.TryParse<TripStatus>(item.Status, out var status))
            {
                counts[status] = item.Count;
            }
        }

        return counts;
    }

    public async Task<bool> TryAdvanceAsync(string tripId, TripStatus from, TripStatus to, CancellationToken cancellationToken)
    {
        var filter = Builders<TripDocument>.Filter.And(
            Builders<TripDocument>.Filter.Eq(candidate => candidate.TripId, tripId),
            Builders<TripDocument>.Filter.Eq(candidate => candidate.Status, WireEnum.ToWire(from)));
        var result = await Collection.UpdateOneAsync(filter, AdvanceUpdate(to, timeProvider.GetUtcNow()), cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    // 状态与版本同一次原子写入。进入终态时记录结束时间并释放司机的执行中占位；
    // 完成时积分引用一并转为 REDEEMED（D3），并补记完成时间（存量行程没有履约事实，其余时间保持为空）。
    private static UpdateDefinition<TripDocument> AdvanceUpdate(TripStatus to, DateTimeOffset now)
    {
        var update = Builders<TripDocument>.Update
            .Set(candidate => candidate.Status, WireEnum.ToWire(to))
            .Inc(VersionedUpdate.VersionField, 1L);
        if (to is not (TripStatus.Completed or TripStatus.Cancelled))
        {
            return update;
        }

        update = update
            .Set(candidate => candidate.ClosedAt, UtcDateTimes.ToStored(now))
            .Set(candidate => candidate.ActiveDriverId, null);

        return to == TripStatus.Completed
            ? update.Set("points.state", WireEnum.ToWire(PointsState.Redeemed)).Set("fulfillment.completedAt", UtcDateTimes.ToStored(now))
            : update;
    }

    // 列表与状态计数共用的筛选：范围、物业、接车时间、积分状态与关键词（含取送地点名称）；不含 status。
    private static FilterDefinition<TripDocument> TripFilter(
        PropertyScope scope,
        string? propertyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? redemptionState,
        string search)
    {
        var filter = Builders<TripDocument>.Filter;

        return new MongoFilterBuilder<TripDocument>(VisibleTo(scope))
            .When(propertyId is not null, () => filter.Eq(candidate => candidate.PropertyId, propertyId))
            .When(from is not null, () => filter.Gte(candidate => candidate.PickupAt, from!.Value.UtcDateTime))
            .When(to is not null, () => filter.Lt(candidate => candidate.PickupAt, to!.Value.UtcDateTime))
            .When(redemptionState is not null, () => filter.Eq("points.state", redemptionState))
            .When(search.Length > 0, () => MongoTextSearch.Contains<TripDocument>(SearchFields, search))
            .Build();
    }

    private static FilterDefinition<TripDocument> ByIdVisibleTo(string tripId, PropertyScope scope) =>
        Builders<TripDocument>.Filter.And(
            Builders<TripDocument>.Filter.Eq(candidate => candidate.TripId, tripId),
            VisibleTo(scope));

    private static FilterDefinition<TripDocument> VisibleTo(PropertyScope scope) =>
        scope.IsAllProperties
            ? FilterDefinition<TripDocument>.Empty
            : Builders<TripDocument>.Filter.Eq(candidate => candidate.PropertyId, scope.PropertyId);
}
