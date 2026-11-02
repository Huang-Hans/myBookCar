using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

// 客户本人维度的订单查询。归属条件 guest.guestId（实际受益宾客）写在存储查询里，不用 bookedBy 判断归属：
// 客服代订给该宾客的订单同样属于该宾客。他人的订单与不存在不作区分。
internal sealed partial class MongoTripRepository
{
    private static readonly TripStatus[] InProgressStatuses =
        [TripStatus.DriverEnRoute, TripStatus.ArrivedAtPickup, TripStatus.InProgress];

    private static readonly TripStatus[] UpcomingStatuses =
        [TripStatus.Pending, TripStatus.Confirmed, TripStatus.DriverAssigned];

    private static readonly TimeSpan CurrentTripGrace = TimeSpan.FromHours(2);

    public async Task<PageResult<Trip>> ListForGuestAsync(ListGuestTripsQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            GuestGroupFilter(query.GuestId, query.Group),
            MongoSortFactory.Create<TripDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<Trip>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<Trip?> FindForGuestAsync(string tripId, string guestId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(ByIdOfGuest(tripId, guestId)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Trip?> FindForGuestAsync(
        string tripId,
        string guestId,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, ByIdOfGuest(tripId, guestId)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    // 进行中优先；没有时取最近的待出发行程（允许略早于当前时间，避免刚到点的订单立即消失）。
    public async Task<Trip?> FindCurrentForGuestAsync(string guestId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var filter = Builders<TripDocument>.Filter;
        var inProgress = await Collection
            .Find(filter.And(GuestOf(guestId), StatusIn(InProgressStatuses)))
            .SortByDescending(candidate => candidate.PickupAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (inProgress is not null)
        {
            return inProgress.ToDomain();
        }

        var earliest = UtcDateTimes.ToStored(now - CurrentTripGrace);
        var upcoming = await Collection
            .Find(filter.And(GuestOf(guestId), StatusIn(UpcomingStatuses), filter.Gte(candidate => candidate.PickupAt, earliest)))
            .SortBy(candidate => candidate.PickupAt)
            .FirstOrDefaultAsync(cancellationToken);

        return upcoming?.ToDomain();
    }

    public async Task<long> CountActiveForGuestAsync(string guestId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var filter = Builders<TripDocument>.Filter;

        return await Collection.CountDocumentsAsync(
            filter.And(
                GuestOf(guestId),
                NotTerminal(),
                filter.Gte(candidate => candidate.PickupAt, UtcDateTimes.ToStored(fromUtc)),
                filter.Lt(candidate => candidate.PickupAt, UtcDateTimes.ToStored(toUtc))),
            cancellationToken: cancellationToken);
    }

    private static FilterDefinition<TripDocument> GuestOf(string guestId) =>
        Builders<TripDocument>.Filter.Eq("guest.guestId", guestId);

    private static FilterDefinition<TripDocument> StatusIn(IEnumerable<TripStatus> statuses) =>
        Builders<TripDocument>.Filter.In(candidate => candidate.Status, statuses.Select(WireEnum.ToWire).ToList());

    private static FilterDefinition<TripDocument> NotTerminal() =>
        Builders<TripDocument>.Filter.Nin(candidate => candidate.Status, TerminalStatuses.Select(WireEnum.ToWire).ToList());

    private static FilterDefinition<TripDocument> ByIdOfGuest(string tripId, string guestId) =>
        Builders<TripDocument>.Filter.And(Builders<TripDocument>.Filter.Eq(candidate => candidate.TripId, tripId), GuestOf(guestId));

    private static FilterDefinition<TripDocument> GuestGroupFilter(string guestId, GuestTripGroup group)
    {
        var filter = Builders<TripDocument>.Filter;

        return group switch
        {
            GuestTripGroup.Completed => filter.And(GuestOf(guestId), StatusIn([TripStatus.Completed])),
            GuestTripGroup.Cancelled => filter.And(GuestOf(guestId), StatusIn([TripStatus.Cancelled])),
            _ => filter.And(GuestOf(guestId), NotTerminal())
        };
    }
}
