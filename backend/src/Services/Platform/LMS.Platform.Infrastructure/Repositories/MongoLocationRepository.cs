using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Locations.Models;
using LMS.Platform.Application.Locations.Ports;
using LMS.Platform.Domain.Locations;
using LMS.Platform.Infrastructure.Documents;
using LMS.Platform.Infrastructure.Persistence;
using LMS.Platform.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

internal sealed class MongoLocationRepository(IMongoDatabase database) : ILocationRepository
{
    private static readonly string[] SearchFields =
        ["_id", "name", "address", "region", "categoryCode", "contactPerson", "contactPhone", "operatingHours"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["locationId"] = "_id",
        ["name"] = "name",
        ["region"] = "region",
        ["categoryCode"] = "categoryCode",
        ["address"] = "address",
        ["coordinates.latitude"] = "coordinates.latitude",
        ["coordinates.longitude"] = "coordinates.longitude",
        ["contactPerson"] = "contactPerson",
        ["contactPhone"] = "contactPhone",
        ["operatingHours"] = "operatingHours",
        ["geofence.isEnabled"] = "geofence.isEnabled",
        ["isActive"] = "isActive",
        ["sortOrder"] = "sortOrder"
    };

    private IMongoCollection<CommonLocationDocument> Collection =>
        database.GetCollection<CommonLocationDocument>(PlatformCollections.CommonLocations);

    public async Task<CommonLocation?> FindByIdAsync(string locationId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.LocationId == locationId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<CommonLocation?> FindAsync(string locationId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, candidate => candidate.LocationId == locationId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PageResult<CommonLocation>> ListAsync(ListLocationsQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<CommonLocationDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<CommonLocation>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public Task InsertAsync(CommonLocation location, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), CommonLocationDocument.FromDomain(location), cancellationToken);

    public Task<bool> TryReplaceAsync(
        CommonLocation updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.LocationId,
            expectedVersion,
            CommonLocationDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);

    public async Task<int> NextSortOrderAsync(LocationRegion region, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var regionCode = WireEnum.ToWire(region);
        var highest = await Collection
            .Find(session, candidate => candidate.Region == regionCode)
            .SortByDescending(candidate => candidate.SortOrder)
            .Limit(1)
            .Project(candidate => candidate.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);

        return highest + 1;
    }

    public async Task<IReadOnlyList<CommonLocation>> ListByRegionAsync(
        LocationRegion region,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var regionCode = WireEnum.ToWire(region);
        var documents = await Collection
            .Find(session, candidate => candidate.Region == regionCode)
            .SortBy(candidate => candidate.SortOrder)
            .ThenBy(candidate => candidate.LocationId)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public Task<bool> TrySetSortOrderAsync(
        string locationId,
        long expectedVersion,
        int sortOrder,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        VersionedUpdate.TryApplyAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            Builders<CommonLocationDocument>.Filter.Eq(candidate => candidate.LocationId, locationId),
            expectedVersion,
            Builders<CommonLocationDocument>.Update.Set(candidate => candidate.SortOrder, sortOrder),
            cancellationToken);

    private static FilterDefinition<CommonLocationDocument> BuildFilter(ListLocationsQuery query)
    {
        var filter = Builders<CommonLocationDocument>.Filter;

        return new MongoFilterBuilder<CommonLocationDocument>(FilterDefinition<CommonLocationDocument>.Empty)
            .When(query.IsActive is not null, () => filter.Eq(candidate => candidate.IsActive, query.IsActive!.Value))
            .When(query.Region is not null, () => filter.Eq(candidate => candidate.Region, WireEnum.ToWire(query.Region!.Value)))
            .When(query.CategoryCode is not null, () => filter.Eq(candidate => candidate.CategoryCode, query.CategoryCode))
            .When(query.LocationIds is not null, () => filter.In(candidate => candidate.LocationId, query.LocationIds))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<CommonLocationDocument>(SearchFields, query.List.Search))
            .Build();
    }
}
