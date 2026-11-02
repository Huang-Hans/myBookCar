using LMS.Booking.Domain.Customers;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class LocationFavoriteDocument
{
    [BsonElement("locationId")]
    public string LocationId { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("region")]
    public string Region { get; init; } = string.Empty;

    public static LocationFavoriteDocument FromDomain(LocationFavorite favorite) =>
        new() { LocationId = favorite.LocationId, Name = favorite.Name, Region = favorite.Region };

    public LocationFavorite ToDomain() => new(LocationId, Name, Region);
}
