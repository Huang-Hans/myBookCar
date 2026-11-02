using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Drivers.Models;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Vehicles;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed class MongoDriverRepository(IMongoDatabase database) : IDriverRepository
{
    private static readonly string[] SearchFields = ["_id", "name", "licenseNumber", "phone", "propertyId", "assignedVehicle"];

    private static readonly string[] CandidateSearchFields = ["name", "assignedVehicle"];

    private static readonly IReadOnlyDictionary<string, string> CandidateSortFields = new Dictionary<string, string>
    {
        ["name"] = "name",
        ["driverId"] = "_id"
    };

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["driverId"] = "_id",
        ["name"] = "name",
        ["licenseNumber"] = "licenseNumber",
        ["phone"] = "phone",
        ["propertyId"] = "propertyId",
        ["assignedVehicle"] = "assignedVehicle",
        ["availability"] = "availability",
        ["status"] = "status",
        ["restriction.reason"] = "restriction.reason",
        ["restriction.effectiveDate"] = "restriction.effectiveDate"
    };

    private IMongoCollection<DriverDocument> Collection => database.GetCollection<DriverDocument>(DispatchCollections.Drivers);

    public async Task<Driver?> FindByIdAsync(string driverId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.DriverId == driverId && candidate.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

        return await DriverVehicleLabels.OneAsync(database, document, cancellationToken);
    }

    public async Task<Driver?> FindVisibleAsync(string driverId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(ByIdVisibleTo(driverId, scope)).FirstOrDefaultAsync(cancellationToken);

        return await DriverVehicleLabels.OneAsync(database, document, cancellationToken);
    }

    public async Task<Driver?> FindVisibleAsync(
        string driverId,
        PropertyScope scope,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, ByIdVisibleTo(driverId, scope)).FirstOrDefaultAsync(cancellationToken);

        return await DriverVehicleLabels.OneAsync(database, document, cancellationToken, session);
    }

    public async Task<IReadOnlyList<Driver>> FindVisibleByIdsAsync(
        IReadOnlyList<string> driverIds,
        PropertyScope scope,
        CancellationToken cancellationToken)
    {
        var filter = Builders<DriverDocument>.Filter.And(Builders<DriverDocument>.Filter.In(candidate => candidate.DriverId, driverIds), VisibleTo(scope));
        var documents = await Collection.Find(filter).ToListAsync(cancellationToken);

        return await DriverVehicleLabels.ManyAsync(database, documents, cancellationToken);
    }

    public async Task<IReadOnlyList<Driver>> FindByIdsAsync(IReadOnlyList<string> driverIds, CancellationToken cancellationToken)
    {
        if (driverIds.Count == 0)
        {
            return [];
        }

        var documents = await Collection.Find(Builders<DriverDocument>.Filter.In(candidate => candidate.DriverId, driverIds)).ToListAsync(cancellationToken);

        return await DriverVehicleLabels.ManyAsync(database, documents, cancellationToken);
    }

    public async Task<IReadOnlyList<Driver>> FindVisibleByIdsAsync(IReadOnlyList<string> driverIds,
        PropertyScope scope, ITransactionContext? context, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverDocument>.Filter.And(
            Builders<DriverDocument>.Filter.In(candidate => candidate.DriverId, driverIds), VisibleTo(scope));
        var query = context is null ? Collection.Find(filter)
            : Collection.Find(MongoTransactionContext.RequireSession(context), filter);
        var documents = await query.ToListAsync(cancellationToken);
        return await DriverVehicleLabels.ManyAsync(database, documents, cancellationToken,
            context is null ? null : MongoTransactionContext.RequireSession(context));
    }

    public async Task<PageResult<Driver>> ListAsync(ListDriversQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<DriverDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<Driver>(await DriverVehicleLabels.ManyAsync(database, page.Items, cancellationToken), page.TotalItems);
    }

    public async Task<PageResult<Driver>> ListDispatchCandidatesAsync(DispatchCandidateQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            Builders<DriverDocument>.Filter.And(CandidateFilter(query), await CandidateVehiclesAsync(query, cancellationToken)),
            MongoSortFactory.Create<DriverDocument>(query.List.Sort, CandidateSortFields),
            query.List,
            cancellationToken);

        return new PageResult<Driver>(await DriverVehicleLabels.ManyAsync(database, page.Items, cancellationToken), page.TotalItems);
    }

    private async Task<FilterDefinition<DriverDocument>> CandidateVehiclesAsync(DispatchCandidateQuery query, CancellationToken cancellationToken)
    {
        var vehicles = database.GetCollection<VehicleDocument>(DispatchCollections.Vehicles);
        var filter = Builders<VehicleDocument>.Filter;
        var matches = filter.And(filter.Eq(item => item.PropertyId, query.PropertyId),
            filter.Eq(item => item.Grade, query.VehicleGrade is { } grade ? WireEnum.ToWire(grade) : string.Empty),
            filter.Ne(item => item.Status, WireEnum.ToWire(VehicleStatus.OutOfService)));
        var eligible = await vehicles.Find(matches).ToListAsync(cancellationToken);
        var drivers = Builders<DriverDocument>.Filter;
        return drivers.Or(drivers.In(item => item.AssignedVehicleId, eligible.Select(item => item.VehicleId)),
            drivers.And(drivers.Eq(item => item.AssignedVehicleId, null),
                drivers.In(item => item.DriverId, eligible.Where(item => item.DriverId is not null).Select(item => item.DriverId!))));
    }

    public async Task InsertAsync(Driver driver, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        await MongoInsert.OneAsync(Collection, session, DriverDocument.FromDomain(driver), cancellationToken);
        await DriverVehicleBindingWriter.FromDriverAsync(database, driver, null, session, cancellationToken);
    }

    // 整体替换会丢掉不属于领域模型的字段，因此先在同一事务内读出本地登录凭据并带入替换文档：业务更新不得清空 password。
    public async Task<bool> TryReplaceAsync(
        Driver updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var replacement = DriverDocument.FromDomain(updated with { Version = expectedVersion + 1 });
        var previous = await Collection
            .Find(session, candidate => candidate.DriverId == updated.DriverId)
            .FirstOrDefaultAsync(cancellationToken);
        replacement.Password = previous?.Password;

        var replaced = await MongoReplace.VersionedAsync(Collection, session, updated.DriverId, expectedVersion, replacement, cancellationToken);
        if (replaced) await DriverVehicleBindingWriter.FromDriverAsync(database, updated, previous, session, cancellationToken);
        return replaced;
    }

    private static FilterDefinition<DriverDocument> ByIdVisibleTo(string driverId, PropertyScope scope) =>
        Builders<DriverDocument>.Filter.And(
            Builders<DriverDocument>.Filter.Eq(candidate => candidate.DriverId, driverId),
            VisibleTo(scope));

    private static FilterDefinition<DriverDocument> VisibleTo(PropertyScope scope) =>
        Builders<DriverDocument>.Filter.And(Builders<DriverDocument>.Filter.Eq(candidate => candidate.DeletedAt, null), scope.IsAllProperties
            ? FilterDefinition<DriverDocument>.Empty
            : Builders<DriverDocument>.Filter.Eq(candidate => candidate.PropertyId, scope.PropertyId));

    // 与 DispatchCandidateRules 一致：同物业、启用、AVAILABLE；没有可用状态值的司机不是候选。
    private static FilterDefinition<DriverDocument> CandidateFilter(DispatchCandidateQuery query)
    {
        var filter = Builders<DriverDocument>.Filter;
        var eligible = filter.And(
            filter.Eq(candidate => candidate.DeletedAt, null),
            filter.Eq(candidate => candidate.PropertyId, query.PropertyId),
            filter.Eq(candidate => candidate.Status, WireEnum.ToWire(DriverStatus.Active)),
            filter.Eq(candidate => candidate.Availability, WireEnum.ToWire(DriverAvailability.Available)),
            filter.Nin(candidate => candidate.DriverId, query.ExcludedDriverIds));

        return new MongoFilterBuilder<DriverDocument>(eligible)
            .When(query.ScheduledDriverIds is not null, () => filter.In(candidate => candidate.DriverId, query.ScheduledDriverIds!))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<DriverDocument>(CandidateSearchFields, query.List.Search))
            .Build();
    }

    private static FilterDefinition<DriverDocument> BuildFilter(ListDriversQuery query)
    {
        var filter = Builders<DriverDocument>.Filter;

        return new MongoFilterBuilder<DriverDocument>(filter.And(VisibleTo(query.Scope), ViewFilter(query.View)))
            .When(query.PropertyId is not null, () => filter.Eq(candidate => candidate.PropertyId, query.PropertyId))
            .When(query.Status is not null, () => filter.Eq(candidate => candidate.Status, WireEnum.ToWire(query.Status!.Value)))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<DriverDocument>(SearchFields, query.List.Search))
            .Build();
    }

    // 页签与其他筛选取交集：可用 / 行程中按只读可用状态，黑名单按停用或暂停限制。
    private static FilterDefinition<DriverDocument> ViewFilter(DriverViewFilter view)
    {
        var filter = Builders<DriverDocument>.Filter;

        return view switch
        {
            DriverViewFilter.Available => filter.Eq(candidate => candidate.Availability, WireEnum.ToWire(DriverAvailability.Available)),
            DriverViewFilter.OnTrip => filter.Eq(candidate => candidate.Availability, WireEnum.ToWire(DriverAvailability.OnTrip)),
            DriverViewFilter.Blacklist => BlacklistFilter(),
            _ => FilterDefinition<DriverDocument>.Empty
        };
    }

    // 黑名单 = 有限制记录，或状态为停用 / 暂停；后者兜住尚未写入限制记录的存量司机。
    private static FilterDefinition<DriverDocument> BlacklistFilter()
    {
        var filter = Builders<DriverDocument>.Filter;
        var restrictedStatuses = DriverRestriction.RestrictedStatuses.Select(WireEnum.ToWire).ToList();

        return filter.Or(
            filter.Ne(candidate => candidate.Restriction, null),
            filter.In(candidate => candidate.Status, restrictedStatuses));
    }
}
