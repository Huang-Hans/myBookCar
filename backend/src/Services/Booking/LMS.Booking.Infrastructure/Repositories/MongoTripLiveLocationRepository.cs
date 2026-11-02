using LMS.Booking.Application.TripLocations.Ports;
using LMS.Booking.Domain.TripLocations;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

// 最新位置的条件写入：只按字段更新（$set），不整体替换，保留 lastPushedAt；序号不大于已存值的点被丢弃。
internal sealed class MongoTripLiveLocationRepository(IMongoDatabase database) : ITripLiveLocationRepository
{
    private static readonly FilterDefinitionBuilder<TripLiveLocationDocument> Filter = Builders<TripLiveLocationDocument>.Filter;
    private static readonly UpdateDefinitionBuilder<TripLiveLocationDocument> Update = Builders<TripLiveLocationDocument>.Update;

    private IMongoCollection<TripLiveLocationDocument> Collection =>
        database.GetCollection<TripLiveLocationDocument>(BookingCollections.TripLiveLocations);

    public async Task<TripLiveLocation?> FindAsync(string tripId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(Filter.Eq(candidate => candidate.TripId, tripId)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    // 记录已存在且序号不小于候选点时，条件不成立而 upsert 尝试插入同一 _id，主键冲突即表示"已有更新的位置"。
    public async Task<TripLiveLocation> SaveIfNewerAsync(TripLiveLocation candidate, CancellationToken cancellationToken)
    {
        var filter = Filter.And(Filter.Eq(document => document.TripId, candidate.TripId), Filter.Lt(document => document.Seq, candidate.Point.Seq));
        try
        {
            await Collection.UpdateOneAsync(filter, ToUpdate(candidate), new UpdateOptions { IsUpsert = true }, cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // 已有序号更大的位置，保持不变。
        }

        return (await FindAsync(candidate.TripId, cancellationToken))!;
    }

    public async Task<bool> TryClaimPushAsync(string tripId, DateTimeOffset pushedBefore, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var filter = Filter.And(
            Filter.Eq(candidate => candidate.TripId, tripId),
            Filter.Or(
                Filter.Eq(candidate => candidate.LastPushedAt, null),
                Filter.Lte(candidate => candidate.LastPushedAt, UtcDateTimes.ToStored(pushedBefore))));
        var result = await Collection.UpdateOneAsync(filter, Update.Set(candidate => candidate.LastPushedAt, UtcDateTimes.ToStored(now)), cancellationToken: cancellationToken);

        return result.ModifiedCount == 1;
    }

    private static UpdateDefinition<TripLiveLocationDocument> ToUpdate(TripLiveLocation location)
    {
        var point = location.Point;

        return Update
            .Set(document => document.DriverId, location.DriverId)
            .Set(document => document.PropertyId, location.PropertyId)
            .Set(document => document.Seq, point.Seq)
            .Set(document => document.Latitude, point.Latitude)
            .Set(document => document.Longitude, point.Longitude)
            .Set(document => document.AccuracyMeters, point.AccuracyMeters)
            .Set(document => document.HeadingDegrees, point.HeadingDegrees)
            .Set(document => document.SpeedKmh, point.SpeedKmh)
            .Set(document => document.RecordedAt, UtcDateTimes.ToStored(point.RecordedAt))
            .Set(document => document.ReceivedAt, UtcDateTimes.ToStored(location.ReceivedAt))
            .Set(document => document.ExpiresAt, UtcDateTimes.ToStored(location.ExpiresAt));
    }
}
