using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Domain.Shifts;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed partial class MongoDriverShiftRepository(IMongoDatabase database) : IDriverShiftRepository, IDriverShiftPeriodReader
{
    private IMongoCollection<DriverShiftDocument> Collection => database.GetCollection<DriverShiftDocument>(DispatchCollections.DriverShifts);
    private static FilterDefinition<DriverShiftDocument> Active => Builders<DriverShiftDocument>.Filter.Eq(shift => shift.WithdrawnAt, null);
    private static readonly string[] DriverSearchFields = [DriverShiftReadQuery.PropertyField, DriverShiftReadQuery.VehiclePlateField];
    private static readonly IReadOnlyDictionary<string, string> DriverSortFields = new Dictionary<string, string>
    {
        ["businessDate"] = "businessDate", ["propertyId"] = DriverShiftReadQuery.PropertyField,
        ["period"] = "period", ["shiftId"] = "_id"
    };

    public async Task<IReadOnlyList<DriverShift>> ListForDriversBetweenAsync(IReadOnlyList<string> driverIds,
        DateOnly from, DateOnly to, ShiftPeriod period, ITransactionContext? context, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverShiftDocument>.Filter;
        var matches = filter.And(Active, filter.In(shift => shift.DriverId, driverIds),
            filter.Eq(shift => shift.Period, WireEnum.ToWire(period)), Between(from, to));
        return await ReadAsync(matches, PropertyScope.AllProperties, null, context, cancellationToken);
    }

    public Task<IReadOnlyList<DriverShift>> ListOnDutyAsync(DateOnly businessDate, ShiftPeriod period,
        PropertyScope scope, string? propertyId, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverShiftDocument>.Filter;
        var matches = filter.And(Active, filter.Eq(shift => shift.BusinessDate, StoredValues.FromDate(businessDate)),
            filter.Eq(shift => shift.Period, WireEnum.ToWire(period)),
            filter.Eq(shift => shift.DutyStatus, WireEnum.ToWire(ShiftDutyStatus.OnDuty)));
        return ReadAsync(matches, scope, propertyId, null, cancellationToken);
    }

    public Task InsertManyAsync(IReadOnlyList<DriverShift> shifts, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.ManyAsync(Collection, MongoTransactionContext.RequireSession(context),
            shifts.Select(DriverShiftDocument.FromDomain).ToList(), cancellationToken);

    public Task<DriverShift?> FindByIdAsync(string shiftId, CancellationToken cancellationToken) =>
        ReadOneAsync(ById(shiftId), PropertyScope.AllProperties, null, cancellationToken);

    public Task<DriverShift?> FindVisibleAsync(string shiftId, PropertyScope scope, CancellationToken cancellationToken) =>
        ReadOneAsync(Builders<DriverShiftDocument>.Filter.And(Active, ById(shiftId)), scope, null, cancellationToken);

    public Task<DriverShift?> FindVisibleAsync(string shiftId, PropertyScope scope,
        ITransactionContext context, CancellationToken cancellationToken) =>
        ReadOneAsync(Builders<DriverShiftDocument>.Filter.And(Active, ById(shiftId)), scope, context, cancellationToken);

    public Task<DriverShift?> FindByDriverInPeriodAsync(string driverId, DateOnly businessDate,
        ShiftPeriod period, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverShiftDocument>.Filter;
        var matches = filter.And(Active, filter.Eq(shift => shift.DriverId, driverId),
            filter.Eq(shift => shift.BusinessDate, StoredValues.FromDate(businessDate)),
            filter.Eq(shift => shift.Period, WireEnum.ToWire(period)));
        return ReadOneAsync(matches, PropertyScope.AllProperties, null, cancellationToken);
    }

    public async Task<PageResult<DriverShift>> ListForDriverAsync(string driverId, PropertyScope scope,
        ListQuery list, CancellationToken cancellationToken)
    {
        var matches = Builders<DriverShiftDocument>.Filter.And(Active, Builders<DriverShiftDocument>.Filter.Eq(shift => shift.DriverId, driverId));
        var query = Query(matches, scope, null, null);
        if (list.HasSearch) query = query.Match(MongoTextSearch.Contains<BsonDocument>(DriverSearchFields, list.Search));
        var count = await query.Count().FirstOrDefaultAsync(cancellationToken);
        var documents = await query.Sort(MongoSortFactory.Create<BsonDocument>(list.Sort, DriverSortFields))
            .Skip(list.Skip).Limit(list.PageSize).ToListAsync(cancellationToken);
        return new(documents.Select(DriverShiftReadQuery.ToDomain).ToList(), checked((int)(count?.Count ?? 0)));
    }

    public Task<IReadOnlyList<DriverShift>> ListForBoardAsync(DateOnly businessDate, PropertyScope scope,
        string? propertyId, CancellationToken cancellationToken) =>
        ReadAsync(Builders<DriverShiftDocument>.Filter.And(Active,
            Builders<DriverShiftDocument>.Filter.Eq(shift => shift.BusinessDate, StoredValues.FromDate(businessDate))),
            scope, propertyId, null, cancellationToken);

    public Task<IReadOnlyList<DriverShift>> ListBetweenAsync(DateOnly from, DateOnly to, PropertyScope scope,
        string? propertyId, CancellationToken cancellationToken) =>
        ReadAsync(Builders<DriverShiftDocument>.Filter.And(Active, Between(from, to)), scope, propertyId, null, cancellationToken);

    public async Task<bool> IsVehicleAssignedAsync(DateOnly businessDate, ShiftPeriod period, string vehiclePlate,
        string exceptShiftId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var filter = Builders<DriverShiftDocument>.Filter;
        var matches = filter.And(Active, filter.Eq(shift => shift.BusinessDate, StoredValues.FromDate(businessDate)),
            filter.Eq(shift => shift.Period, WireEnum.ToWire(period)), filter.Ne(shift => shift.ShiftId, exceptShiftId));
        var taken = Builders<BsonDocument>.Filter.Eq(DriverShiftReadQuery.VehiclePlateField, vehiclePlate);
        return await Query(matches, PropertyScope.AllProperties, null, context).Match(taken).Limit(1).AnyAsync(cancellationToken);
    }

    public Task InsertAsync(DriverShift shift, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(Collection, MongoTransactionContext.RequireSession(context), DriverShiftDocument.FromDomain(shift), cancellationToken);

    public Task<bool> TryReplaceAsync(DriverShift updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(Collection, MongoTransactionContext.RequireSession(context), updated.ShiftId,
            expectedVersion, DriverShiftDocument.FromDomain(updated with { Version = expectedVersion + 1 }), cancellationToken);

    private IAggregateFluent<BsonDocument> Query(FilterDefinition<DriverShiftDocument> matches, PropertyScope scope,
        string? propertyId, ITransactionContext? context) => DriverShiftReadQuery.Create(database, matches, scope, propertyId,
            context is null ? null : MongoTransactionContext.RequireSession(context));

    private async Task<DriverShift?> ReadOneAsync(FilterDefinition<DriverShiftDocument> matches,
        PropertyScope scope, ITransactionContext? context, CancellationToken cancellationToken)
    {
        var document = await Query(matches, scope, null, context).Limit(1).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : DriverShiftReadQuery.ToDomain(document);
    }

    private async Task<IReadOnlyList<DriverShift>> ReadAsync(FilterDefinition<DriverShiftDocument> matches,
        PropertyScope scope, string? propertyId, ITransactionContext? context, CancellationToken cancellationToken)
    {
        var documents = await Query(matches, scope, propertyId, context).ToListAsync(cancellationToken);
        return documents.Select(DriverShiftReadQuery.ToDomain).ToList();
    }

    private static FilterDefinition<DriverShiftDocument> ById(string shiftId) =>
        Builders<DriverShiftDocument>.Filter.Eq(shift => shift.ShiftId, shiftId);

    private static FilterDefinition<DriverShiftDocument> Between(DateOnly from, DateOnly to) =>
        Builders<DriverShiftDocument>.Filter.And(
            Builders<DriverShiftDocument>.Filter.Gte(shift => shift.BusinessDate, StoredValues.FromDate(from)),
            Builders<DriverShiftDocument>.Filter.Lte(shift => shift.BusinessDate, StoredValues.FromDate(to)));
}
