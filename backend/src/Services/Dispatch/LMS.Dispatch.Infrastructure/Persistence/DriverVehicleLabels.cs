using LMS.Dispatch.Domain.Vehicles;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Infrastructure.Documents;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Persistence;

internal static class DriverVehicleLabels
{
    public static async Task<Driver?> OneAsync(IMongoDatabase database, DriverDocument? document,
        CancellationToken cancellationToken, IClientSessionHandle? session = null)
    {
        if (document is null) return null;
        var resolved = await ManyAsync(database, [document], cancellationToken, session);
        return resolved[0];
    }

    public static async Task<IReadOnlyList<Driver>> ManyAsync(IMongoDatabase database,
        IReadOnlyList<DriverDocument> documents, CancellationToken cancellationToken, IClientSessionHandle? session = null)
    {
        var ids = documents.Select(document => document.AssignedVehicleId).OfType<string>().Distinct().ToList();
        if (documents.Count == 0) return [];
        var driverIds = documents.Where(document => document.AssignedVehicleId is null).Select(document => document.DriverId).ToList();
        var collection = database.GetCollection<VehicleDocument>(DispatchCollections.Vehicles);
        var filter = Builders<VehicleDocument>.Filter;
        var linked = filter.Or(filter.In(vehicle => vehicle.VehicleId, ids), filter.In(vehicle => vehicle.DriverId, driverIds));
        var query = session is null ? collection.Find(linked) : collection.Find(session, linked);
        var vehicles = await query.ToListAsync(cancellationToken);
        var byId = vehicles.ToDictionary(vehicle => vehicle.VehicleId, StringComparer.Ordinal);
        var byDriver = vehicles.Where(vehicle => vehicle.DriverId is not null).ToDictionary(vehicle => vehicle.DriverId!, StringComparer.Ordinal);
        return documents.Select(document => Resolve(document, byId, byDriver)).ToList();
    }

    private static Driver Resolve(DriverDocument document, IReadOnlyDictionary<string, VehicleDocument> vehicles,
        IReadOnlyDictionary<string, VehicleDocument> byDriver)
    {
        var driver = document.ToDomain();
        var vehicle = driver.AssignedVehicleId is null ? byDriver.GetValueOrDefault(driver.DriverId)
            : vehicles.GetValueOrDefault(driver.AssignedVehicleId);
        if (vehicle is null || vehicle.PropertyId != driver.PropertyId) return driver;
        return driver with { AssignedVehicleId = vehicle.VehicleId, AssignedVehicle = $"{vehicle.Plate} · {vehicle.Model}",
            AssignedVehicleGrade = vehicle.Status == WireEnum.ToWire(VehicleStatus.OutOfService) ? null
                : StoredValues.ParseEnum<VehicleGrade>(vehicle.Grade, "grade") };
    }
}
