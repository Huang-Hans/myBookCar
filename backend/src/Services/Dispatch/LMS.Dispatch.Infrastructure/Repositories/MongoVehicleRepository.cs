using System.Text.RegularExpressions;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Vehicles.Models;
using LMS.Dispatch.Application.Vehicles.Ports;
using LMS.Dispatch.Domain.Vehicles;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed class MongoVehicleRepository(IMongoDatabase database) : IVehicleRepository
{
    private static readonly string[] SearchFields = ["plate", "model"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["vehicleId"] = "_id",
        ["plate"] = "plate",
        ["model"] = "model",
        ["propertyId"] = "propertyId",
        ["grade"] = "grade",
        ["seats"] = "seats",
        ["mileageKm"] = "mileageKm",
        ["status"] = "status"
    };

    private IMongoCollection<VehicleDocument> Collection => database.GetCollection<VehicleDocument>(DispatchCollections.Vehicles);

    public async Task<Vehicle?> FindByIdAsync(string vehicleId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.VehicleId == vehicleId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Vehicle?> FindVisibleAsync(string vehicleId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(ByIdVisibleTo(vehicleId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Vehicle?> FindByPlateAsync(string plate, CancellationToken cancellationToken)
    {
        var pattern = new BsonRegularExpression($"^{Regex.Escape(plate)}$", "i");
        var document = await Collection.Find(Builders<VehicleDocument>.Filter.Regex(candidate => candidate.Plate, pattern)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task<bool> IsDriverAssignedAsync(string driverId, CancellationToken cancellationToken) =>
        Collection.Find(candidate => candidate.DriverId == driverId).AnyAsync(cancellationToken);

    public async Task<PageResult<Vehicle>> ListAsync(ListVehiclesQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<VehicleDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<Vehicle>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<VehicleStatusCounts> CountByStatusAsync(PropertyScope scope, string? propertyId, CancellationToken cancellationToken)
    {
        var counts = await Collection.Aggregate()
            .Match(VisibleFilter(scope, propertyId))
            .Group(document => document.Status, group => new StatusCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
        var byStatus = counts.ToDictionary(
            count => StoredValues.ParseEnum<VehicleStatus>(count.Status, "status"),
            count => (long)count.Count);

        return new VehicleStatusCounts(
            Count(byStatus, VehicleStatus.Available),
            Count(byStatus, VehicleStatus.OnTrip),
            Count(byStatus, VehicleStatus.Maintenance),
            Count(byStatus, VehicleStatus.OutOfService));
    }

    public async Task InsertAsync(Vehicle vehicle, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        await MongoInsert.OneAsync(Collection, session, VehicleDocument.FromDomain(vehicle), cancellationToken);
        await DriverVehicleBindingWriter.FromVehicleAsync(database, vehicle, session, cancellationToken);
    }

    private static long Count(IReadOnlyDictionary<VehicleStatus, long> counts, VehicleStatus status) =>
        counts.TryGetValue(status, out var count) ? count : 0;

    private static FilterDefinition<VehicleDocument> BuildFilter(ListVehiclesQuery query)
    {
        var filter = Builders<VehicleDocument>.Filter;

        return new MongoFilterBuilder<VehicleDocument>(VisibleFilter(query.Scope, query.PropertyId))
            .When(query.Grade is not null, () => filter.Eq(candidate => candidate.Grade, WireEnum.ToWire(query.Grade!.Value)))
            .When(query.Status is not null, () => filter.Eq(candidate => candidate.Status, WireEnum.ToWire(query.Status!.Value)))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<VehicleDocument>(SearchFields, query.List.Search))
            .Build();
    }

    private static FilterDefinition<VehicleDocument> VisibleFilter(PropertyScope scope, string? propertyId)
    {
        var filter = Builders<VehicleDocument>.Filter;
        var visible = scope.IsAllProperties
            ? FilterDefinition<VehicleDocument>.Empty
            : filter.Eq(candidate => candidate.PropertyId, scope.PropertyId);

        return propertyId is null ? visible : filter.And(visible, filter.Eq(candidate => candidate.PropertyId, propertyId));
    }

    private static FilterDefinition<VehicleDocument> ByIdVisibleTo(string vehicleId, PropertyScope scope) =>
        Builders<VehicleDocument>.Filter.And(
            Builders<VehicleDocument>.Filter.Eq(candidate => candidate.VehicleId, vehicleId),
            VisibleFilter(scope, null));

    private sealed record StatusCount(string Status, int Count);
}
