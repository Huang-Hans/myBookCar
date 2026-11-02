using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Dispatch.Application.Offers.Models;
using LMS.Dispatch.Domain.Offers;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

// 客服派单需要的邀请读取：最近一轮、占用中的邀请、过期扫描与邀请历史。
internal sealed partial class MongoDriverOfferRepository
{
    private static readonly IReadOnlyDictionary<string, string> HistorySortFields = new Dictionary<string, string>
    {
        ["offerId"] = "_id",
        ["round"] = "round",
        ["createdAt"] = "createdAt"
    };

    private static readonly string[] OpenStatuses =
        [WireEnum.ToWire(DriverOfferStatus.Pending), WireEnum.ToWire(DriverOfferStatus.Accepting)];

    public async Task<DriverOffer?> FindLatestForTripAsync(string tripId, CancellationToken cancellationToken)
    {
        var document = await Collection
            .Find(candidate => candidate.TripId == tripId)
            .SortByDescending(candidate => candidate.CreatedAt)
            .ThenByDescending(candidate => candidate.OfferId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<DriverOffer>> FindLatestForTripsAsync(
        IReadOnlyList<string> tripIds,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        if (tripIds.Count == 0)
        {
            return [];
        }

        var filter = Builders<DriverOfferDocument>.Filter.And(
            Builders<DriverOfferDocument>.Filter.In(candidate => candidate.TripId, tripIds),
            Builders<DriverOfferDocument>.Filter.Gte(candidate => candidate.CreatedAt, UtcDateTimes.ToStored(since)));
        var documents = await Collection
            .Find(filter)
            .SortByDescending(candidate => candidate.CreatedAt)
            .ThenByDescending(candidate => candidate.OfferId)
            .ToListAsync(cancellationToken);

        return documents.GroupBy(document => document.TripId).Select(group => group.First().ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<OfferState>> ListLatestStatesAsync(
        PropertyScope scope,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        var filter = Builders<DriverOfferDocument>.Filter.And(
            Builders<DriverOfferDocument>.Filter.Gte(candidate => candidate.CreatedAt, UtcDateTimes.ToStored(since)),
            VisibleTo(scope));
        var latest = await Collection
            .Aggregate()
            .Match(filter)
            .SortByDescending(candidate => candidate.CreatedAt)
            .ThenByDescending(candidate => candidate.OfferId)
            .Group(candidate => candidate.TripId, group => new { TripId = group.Key, Status = group.First().Status, ExpiresAt = group.First().ExpiresAt })
            .ToListAsync(cancellationToken);

        return latest
            .Where(item => item.Status != WireEnum.ToWire(DriverOfferStatus.Accepted))
            .Select(item => new OfferState(
                item.TripId, StoredValues.ParseEnum<DriverOfferStatus>(item.Status, "status"), UtcDateTimes.FromStored(item.ExpiresAt)))
            .ToList();
    }

    public async Task<IReadOnlyList<DriverOffer>> ListOpenForTripAsync(string tripId, CancellationToken cancellationToken) =>
        await ListOpenAsync(Builders<DriverOfferDocument>.Filter.Eq(candidate => candidate.TripId, tripId), cancellationToken);

    public async Task<IReadOnlyList<DriverOffer>> ListHeldByDriverAsync(string driverId, CancellationToken cancellationToken) =>
        await ListOpenAsync(Builders<DriverOfferDocument>.Filter.Eq(candidate => candidate.DriverId, driverId), cancellationToken);

    public async Task<IReadOnlyList<string>> ListHeldDriverIdsAsync(string propertyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverOfferDocument>.Filter.And(
            Builders<DriverOfferDocument>.Filter.Eq(candidate => candidate.PropertyId, propertyId),
            LiveFilter(UtcDateTimes.ToStored(now)));

        return await Collection.Find(filter).Project(candidate => candidate.DriverId).ToListAsync(cancellationToken);
    }

    public async Task<PageResult<DriverOffer>> ListForTripAsync(string tripId, ListQuery list, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            Builders<DriverOfferDocument>.Filter.Eq(candidate => candidate.TripId, tripId),
            MongoSortFactory.Create<DriverOfferDocument>(list.Sort, HistorySortFields),
            list,
            cancellationToken);

        return new PageResult<DriverOffer>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<IReadOnlyList<DriverOffer>> ListExpiredPendingAsync(DateTimeOffset now, int take, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverOfferDocument>.Filter.And(
            Builders<DriverOfferDocument>.Filter.Eq(candidate => candidate.Status, WireEnum.ToWire(DriverOfferStatus.Pending)),
            Builders<DriverOfferDocument>.Filter.Lte(candidate => candidate.ExpiresAt, UtcDateTimes.ToStored(now)));
        var documents = await Collection.Find(filter).SortBy(candidate => candidate.ExpiresAt).Limit(take).ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    private async Task<IReadOnlyList<DriverOffer>> ListOpenAsync(FilterDefinition<DriverOfferDocument> owner, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverOfferDocument>.Filter.And(
            owner,
            Builders<DriverOfferDocument>.Filter.In(candidate => candidate.Status, OpenStatuses));
        var documents = await Collection.Find(filter).ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    private static FilterDefinition<DriverOfferDocument> VisibleTo(PropertyScope scope) =>
        scope.IsAllProperties
            ? FilterDefinition<DriverOfferDocument>.Empty
            : Builders<DriverOfferDocument>.Filter.Eq(candidate => candidate.PropertyId, scope.PropertyId);

    // 此刻仍然有效的邀请：待响应且未过期，或处理中。
    private static FilterDefinition<DriverOfferDocument> LiveFilter(DateTime nowStored)
    {
        var filter = Builders<DriverOfferDocument>.Filter;
        var pending = filter.And(
            filter.Eq(candidate => candidate.Status, WireEnum.ToWire(DriverOfferStatus.Pending)),
            filter.Gt(candidate => candidate.ExpiresAt, nowStored));

        return filter.Or(pending, filter.Eq(candidate => candidate.Status, WireEnum.ToWire(DriverOfferStatus.Accepting)));
    }
}
