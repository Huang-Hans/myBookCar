using LMS.Booking.Domain.Trips;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

// 报表来源读取：计划接车时间区间 [from, to) 内的全部行程，按 _id 游标分批，走 ix_pickup 索引。
internal sealed partial class MongoTripRepository
{
    public async Task<IReadOnlyList<Trip>> ListByPickupAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? afterTripId,
        int limit,
        CancellationToken cancellationToken)
    {
        var filter = Builders<TripDocument>.Filter;
        var window = filter.And(
            filter.Gte(document => document.PickupAt, UtcDateTimes.ToStored(from)),
            filter.Lt(document => document.PickupAt, UtcDateTimes.ToStored(to)));
        var scoped = afterTripId is null ? window : filter.And(window, filter.Gt(document => document.TripId, afterTripId));
        var documents = await Collection.Find(scoped)
            .SortBy(document => document.TripId)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }
}
