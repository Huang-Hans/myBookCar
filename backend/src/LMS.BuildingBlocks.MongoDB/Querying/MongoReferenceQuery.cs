using MongoDB.Bson;
using MongoDB.Driver;

namespace LMS.BuildingBlocks.MongoDB.Querying;

public static class MongoReferenceQuery
{
    public static async Task<bool> ExistsAsync(
        IMongoDatabase database, IReadOnlyList<(string Collection, string Field)> references,
        string value, CancellationToken cancellationToken)
    {
        foreach (var reference in references)
        {
            var collection = database.GetCollection<BsonDocument>(reference.Collection);
            var filter = Builders<BsonDocument>.Filter.Eq(reference.Field, value);
            var found = await collection.Find(filter).Project(Builders<BsonDocument>.Projection.Include("_id"))
                .Limit(1).FirstOrDefaultAsync(cancellationToken);
            if (found is not null) return true;
        }
        return false;
    }
}
