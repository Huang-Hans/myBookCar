using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Vehicles;
using LMS.Dispatch.Infrastructure.Documents;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Persistence;

internal static class DriverVehicleBindingWriter
{
    public static async Task FromDriverAsync(IMongoDatabase database, Driver driver, DriverDocument? previous,
        IClientSessionHandle session, CancellationToken cancellationToken)
    {
        if (driver.DeletedAt is null && driver.AssignedVehicleId is not null
            && driver.AssignedVehicleId == previous?.AssignedVehicleId && driver.PropertyId == previous.PropertyId) return;
        var collection = database.GetCollection<VehicleDocument>(DispatchCollections.Vehicles);
        var vehicleId = driver.DeletedAt is null ? driver.AssignedVehicleId : null;
        var target = await ReadTargetAsync(collection, driver, vehicleId, session, cancellationToken);
        await ClearPreviousAsync(collection, driver.DriverId, vehicleId, session, cancellationToken);
        if (target is null) return;
        var update = Builders<VehicleDocument>.Update.Set(vehicle => vehicle.DriverId, driver.DriverId).Inc(vehicle => vehicle.Version, 1);
        await collection.UpdateOneAsync(session, vehicle => vehicle.VehicleId == target.VehicleId, update, cancellationToken: cancellationToken);
    }

    private static async Task<VehicleDocument?> ReadTargetAsync(IMongoCollection<VehicleDocument> collection,
        Driver driver, string? vehicleId, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        if (vehicleId is null) return null;
        var target = await collection.Find(session, vehicle => vehicle.VehicleId == vehicleId && vehicle.PropertyId == driver.PropertyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (target is null || (target.DriverId is not null && target.DriverId != driver.DriverId)) throw Conflict();
        return target;
    }

    private static Task ClearPreviousAsync(IMongoCollection<VehicleDocument> collection, string driverId,
        string? targetId, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var filter = Builders<VehicleDocument>.Filter;
        var previous = filter.And(filter.Eq(vehicle => vehicle.DriverId, driverId), filter.Ne(vehicle => vehicle.VehicleId, targetId));
        var update = Builders<VehicleDocument>.Update.Unset(vehicle => vehicle.DriverId).Inc(vehicle => vehicle.Version, 1);
        return collection.UpdateManyAsync(session, previous, update, cancellationToken: cancellationToken);
    }

    public static async Task FromVehicleAsync(IMongoDatabase database, Vehicle vehicle,
        IClientSessionHandle session, CancellationToken cancellationToken)
    {
        if (vehicle.DriverId is null) return;
        var collection = database.GetCollection<DriverDocument>(DispatchCollections.Drivers);
        var filter = Builders<DriverDocument>.Filter;
        var eligible = filter.And(filter.Eq(driver => driver.DriverId, vehicle.DriverId),
            filter.Eq(driver => driver.PropertyId, vehicle.PropertyId),
            filter.Eq(driver => driver.DeletedAt, null),
            filter.Eq(driver => driver.Status, WireEnum.ToWire(DriverStatus.Active)), filter.Eq(driver => driver.AssignedVehicleId, null));
        var update = Builders<DriverDocument>.Update.Set(driver => driver.AssignedVehicleId, vehicle.VehicleId)
            .Unset(driver => driver.AssignedVehicle).Inc(driver => driver.Version, 1);
        var result = await collection.UpdateOneAsync(session, eligible, update, cancellationToken: cancellationToken);
        if (result.MatchedCount == 0) throw Conflict();
    }

    private static DuplicateRecordException Conflict() =>
        new("driver vehicle binding", new InvalidOperationException("The vehicle binding changed."));
}
