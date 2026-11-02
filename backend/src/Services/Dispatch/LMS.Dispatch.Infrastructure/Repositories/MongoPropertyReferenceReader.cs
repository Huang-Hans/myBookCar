using LMS.BuildingBlocks.Core.References;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed class MongoPropertyReferenceReader(IMongoDatabase database) : IPropertyReferenceReader
{
    private static readonly (string Collection, string Field)[] References =
    [
        (DispatchCollections.Drivers, "propertyId"),
        (DispatchCollections.Vehicles, "propertyId"),
        (DispatchCollections.Coupons, "propertyId"),
        (DispatchCollections.DriverOffers, "propertyId"),
        (DispatchCollections.DriverInspections, "propertyId"),
        (DispatchCollections.VehicleIssues, "propertyId"),
        (DispatchCollections.DriverIncidents, "propertyId"),
        (DispatchCollections.DriverShifts, "propertyId"),
    ];

    public Task<bool> HasReferencesAsync(string propertyId, CancellationToken cancellationToken) =>
        MongoReferenceQuery.ExistsAsync(database, References, propertyId, cancellationToken);
}
