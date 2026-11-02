using LMS.BuildingBlocks.Core.References;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoPropertyReferenceReader(IMongoDatabase database) : IPropertyReferenceReader
{
    private static readonly (string Collection, string Field)[] References =
    [
        (BookingCollections.PropertyQuotas, "propertyId"),
        (BookingCollections.QuotaOverrides, "propertyId"),
        (BookingCollections.QualificationReferences, "propertyId"),
        (BookingCollections.CrossBorderApprovals, "propertyId"),
        (BookingCollections.Trips, "propertyId"),
        (BookingCollections.Guests, "propertyId"),
        (BookingCollections.ConsoleNotifications, "propertyId"),
        (BookingCollections.TripLiveLocations, "propertyId"),
    ];

    public Task<bool> HasReferencesAsync(string propertyId, CancellationToken cancellationToken) =>
        MongoReferenceQuery.ExistsAsync(database, References, propertyId, cancellationToken);
}
