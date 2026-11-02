using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Domain.Offers;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed partial class MongoDriverOfferRepository(IMongoDatabase database) : IDriverOfferRepository
{
    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["offerId"] = "_id",
        ["expiresAt"] = "expiresAt",
        ["createdAt"] = "createdAt"
    };

    private IMongoCollection<DriverOfferDocument> Collection =>
        database.GetCollection<DriverOfferDocument>(DispatchCollections.DriverOffers);

    public async Task<DriverOffer?> FindByIdAsync(string offerId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.OfferId == offerId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<DriverOffer?> FindForDriverAsync(string offerId, string driverId, CancellationToken cancellationToken)
    {
        var document = await Collection
            .Find(candidate => candidate.OfferId == offerId && candidate.DriverId == driverId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PageResult<DriverOffer>> ListOpenForDriverAsync(
        string driverId,
        DateTimeOffset now,
        ListQuery list,
        CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            OpenFilter(driverId, UtcDateTimes.ToStored(now)),
            MongoSortFactory.Create<DriverOfferDocument>(list.Sort, SortFields),
            list,
            cancellationToken);

        return new PageResult<DriverOffer>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<bool> ExistsOpenAsync(string tripId, string driverId, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverOfferDocument>.Filter.And(
            Builders<DriverOfferDocument>.Filter.Eq(candidate => candidate.TripId, tripId),
            Builders<DriverOfferDocument>.Filter.Eq(candidate => candidate.DriverId, driverId),
            Builders<DriverOfferDocument>.Filter.In(
                candidate => candidate.Status,
                [WireEnum.ToWire(DriverOfferStatus.Pending), WireEnum.ToWire(DriverOfferStatus.Accepting)]));

        return await Collection.Find(filter).AnyAsync(cancellationToken);
    }

    public Task InsertAsync(DriverOffer offer, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(Collection, MongoTransactionContext.RequireSession(context), DriverOfferDocument.FromDomain(offer), cancellationToken);

    public Task<bool> TryReplaceAsync(
        DriverOffer updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.OfferId,
            expectedVersion,
            DriverOfferDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);

    public async Task<bool> TryReplaceAsync(DriverOffer updated, long expectedVersion, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverOfferDocument>.Filter.And(
            Builders<DriverOfferDocument>.Filter.Eq(candidate => candidate.OfferId, updated.OfferId),
            Builders<DriverOfferDocument>.Filter.Eq(VersionedUpdate.VersionField, expectedVersion));
        var replacement = DriverOfferDocument.FromDomain(updated with { Version = expectedVersion + 1 });
        var result = await Collection.ReplaceOneAsync(filter, replacement, cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    // 待响应且未过期，或处理中（等待 Booking 确认）的邀请。
    private static FilterDefinition<DriverOfferDocument> OpenFilter(string driverId, DateTime nowStored)
    {
        var filter = Builders<DriverOfferDocument>.Filter;
        var pending = filter.And(
            filter.Eq(candidate => candidate.Status, WireEnum.ToWire(DriverOfferStatus.Pending)),
            filter.Gt(candidate => candidate.ExpiresAt, nowStored));
        var accepting = filter.Eq(candidate => candidate.Status, WireEnum.ToWire(DriverOfferStatus.Accepting));

        return filter.And(filter.Eq(candidate => candidate.DriverId, driverId), filter.Or(pending, accepting));
    }
}
