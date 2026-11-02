using LMS.BuildingBlocks.Core.References;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Reporting.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Reporting.Infrastructure.Repositories;

internal sealed class MongoPropertyReferenceReader(IMongoDatabase database) : IPropertyReferenceReader
{
    private static readonly (string Collection, string Field)[] References =
    [
        (ReportingCollections.ReportSchedules, "propertyScopes"),
        (ReportingCollections.ReportExports, "propertyScopes"),
        (ReportingCollections.ReportExports, "filter.propertyId"),
        (ReportingCollections.ReportRows, "filter.propertyId"),
    ];

    public Task<bool> HasReferencesAsync(string propertyId, CancellationToken cancellationToken) =>
        MongoReferenceQuery.ExistsAsync(database, References, propertyId, cancellationToken);
}
