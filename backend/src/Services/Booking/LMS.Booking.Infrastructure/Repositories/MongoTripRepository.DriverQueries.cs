using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

// 司机本人维度的行程查询。归属条件 assignment.driverId 写在存储查询里：他人的行程与不存在不作区分，
// 也不按司机姓名推断归属（存量行程没有指派，因此不会出现在任何司机的列表里）。
internal sealed partial class MongoTripRepository
{
    private static readonly TripStatus[] DriverActiveStatuses =
        [TripStatus.DriverAssigned, TripStatus.DriverEnRoute, TripStatus.ArrivedAtPickup, TripStatus.InProgress];

    private static readonly TripStatus[] TerminalStatuses = [TripStatus.Completed, TripStatus.Cancelled];

    public async Task<PageResult<Trip>> ListForDriverAsync(ListDriverTripsQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            DriverListFilter(query),
            MongoSortFactory.Create<TripDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<Trip>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<Trip?> FindForDriverAsync(
        string tripId,
        string driverId,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, ByIdOfDriver(tripId, driverId)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Trip?> FindForDriverAsync(string tripId, string driverId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(ByIdOfDriver(tripId, driverId)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Trip?> FindActiveForDriverAsync(string driverId, CancellationToken cancellationToken)
    {
        var filter = Builders<TripDocument>.Filter.Eq(candidate => candidate.ActiveDriverId, driverId);
        var document = await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    private static FilterDefinition<TripDocument> ByIdOfDriver(string tripId, string driverId) =>
        Builders<TripDocument>.Filter.And(
            Builders<TripDocument>.Filter.Eq(candidate => candidate.TripId, tripId),
            Builders<TripDocument>.Filter.Eq("assignment.driverId", driverId));

    private static FilterDefinition<TripDocument> DriverListFilter(ListDriverTripsQuery query)
    {
        var filter = Builders<TripDocument>.Filter;
        var statuses = query.View == DriverTripView.Active ? DriverActiveStatuses : StatusesOfHistory(query.Status);

        return filter.And(
            filter.Eq("assignment.driverId", query.DriverId),
            filter.In(candidate => candidate.Status, statuses.Select(WireEnum.ToWire).ToList()));
    }

    private static IReadOnlyList<TripStatus> StatusesOfHistory(TripStatus? requested) =>
        requested is { } status && TerminalStatuses.Contains(status) ? [status] : TerminalStatuses;
}
