using LMS.Platform.Domain.Locations;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

internal sealed class CoordinatesDocument
{
    [BsonElement("latitude")]
    public double Latitude { get; init; }

    [BsonElement("longitude")]
    public double Longitude { get; init; }

    public static CoordinatesDocument FromDomain(Coordinates coordinates) =>
        new() { Latitude = coordinates.Latitude, Longitude = coordinates.Longitude };

    public Coordinates ToDomain() => new(Latitude, Longitude);
}
