using LMS.Platform.Application.Configuration.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class LocationCategoryDocument
{
    [BsonElement("categoryCode")]
    public string CategoryCode { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; init; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; init; }

    public LocationCategory ToItem() => new(CategoryCode, Name, Description, IsActive);
}
