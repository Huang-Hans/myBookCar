using LMS.BuildingBlocks.Core.References;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

internal sealed class MongoPropertyReferenceReader(IMongoDatabase database) : IPropertyReferenceReader
{
    private static readonly (string Collection, string Field)[] References =
    [
        (PlatformCollections.PortalUsers, "propertyId"),
        (PlatformCollections.FileMetadata, "propertyId"),
    ];

    public Task<bool> HasReferencesAsync(string propertyId, CancellationToken cancellationToken) =>
        MongoReferenceQuery.ExistsAsync(database, References, propertyId, cancellationToken);
}
