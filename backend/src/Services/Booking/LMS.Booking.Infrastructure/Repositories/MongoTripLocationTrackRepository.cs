using LMS.Booking.Application.TripLocations.Ports;
using LMS.Booking.Domain.TripLocations;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

// 行驶轨迹：无序批量插入，_id（tripId:seq）冲突即该点已保存，忽略后其余点照常写入；按 tripId + seq 索引顺序读取。
internal sealed class MongoTripLocationTrackRepository(IMongoDatabase database) : ITripLocationTrackRepository
{
    private static readonly FilterDefinitionBuilder<TripLocationPointDocument> Filter = Builders<TripLocationPointDocument>.Filter;

    private static readonly InsertManyOptions UnorderedInsert = new() { IsOrdered = false };

    private IMongoCollection<TripLocationPointDocument> Collection =>
        database.GetCollection<TripLocationPointDocument>(BookingCollections.TripLocationPoints);

    public async Task AppendAsync(string tripId, IReadOnlyList<LocationPoint> points, DateTimeOffset receivedAt, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var documents = points.Select(point => TripLocationPointDocument.From(tripId, point, receivedAt, expiresAt));
        try
        {
            await Collection.InsertManyAsync(documents, UnorderedInsert, cancellationToken);
        }
        catch (MongoBulkWriteException exception) when (IsOnlyDuplicates(exception))
        {
            // 重传的点已保存，其余点已写入。
        }
    }

    public async Task<IReadOnlyList<LocationPoint>> ListAsync(string tripId, long afterSeq, CancellationToken cancellationToken)
    {
        var filter = Filter.And(Filter.Eq(document => document.TripId, tripId), Filter.Gt(document => document.Seq, afterSeq));
        var documents = await Collection.Find(filter)
            .SortBy(document => document.Seq)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<LocationPoint>>> ListForTripsAsync(IReadOnlyCollection<string> tripIds, CancellationToken cancellationToken)
    {
        if (tripIds.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<LocationPoint>>();
        }

        var documents = await Collection.Find(Filter.In(document => document.TripId, tripIds))
            .SortBy(document => document.TripId)
            .ThenBy(document => document.Seq)
            .ToListAsync(cancellationToken);

        return documents
            .GroupBy(document => document.TripId)
            .ToDictionary(group => group.Key, IReadOnlyList<LocationPoint> (group) => group.Select(document => document.ToDomain()).ToList());
    }

    private static bool IsOnlyDuplicates(MongoBulkWriteException exception) =>
        exception.WriteConcernError is null
        && exception.WriteErrors.All(error => error.Category == ServerErrorCategory.DuplicateKey);
}
