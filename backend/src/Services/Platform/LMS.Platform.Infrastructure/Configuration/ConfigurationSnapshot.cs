using LMS.Platform.Domain.Configuration;
using MongoDB.Bson;

namespace LMS.Platform.Infrastructure.Configuration;

internal sealed record ConfigurationSnapshot(ActiveConfiguration Active, IReadOnlyList<BsonDocument> Assets)
{
    public BsonArray PropertyItems => Assets.First(asset => asset["areaCode"] == "PROPERTIES")["payload"]["items"].AsBsonArray;
}
