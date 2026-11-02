using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Reviews.Ports;
using LMS.Booking.Domain.Reviews;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoTripReviewRepository(IMongoDatabase database) : ITripReviewRepository
{
    private IMongoCollection<TripReviewDocument> Collection =>
        database.GetCollection<TripReviewDocument>(BookingCollections.TripReviews);

    public async Task<TripReview?> FindAsync(string tripId, string guestId, CancellationToken cancellationToken)
    {
        var document = await Collection
            .Find(candidate => candidate.TripId == tripId && candidate.GuestId == guestId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<TripReview?> FindAsync(string tripId, string guestId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection
            .Find(session, candidate => candidate.TripId == tripId && candidate.GuestId == guestId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<TripReview>> FindForGuestAsync(
        string guestId,
        IReadOnlyList<string> tripIds,
        CancellationToken cancellationToken)
    {
        if (tripIds.Count == 0)
        {
            return [];
        }

        var filter = Builders<TripReviewDocument>.Filter;
        var documents = await Collection
            .Find(filter.And(filter.Eq(candidate => candidate.GuestId, guestId), filter.In(candidate => candidate.TripId, tripIds)))
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public Task InsertAsync(TripReview review, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(Collection, MongoTransactionContext.RequireSession(context), TripReviewDocument.FromDomain(review), cancellationToken);

    public Task<bool> TryReplaceAsync(
        TripReview updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.TripId,
            expectedVersion,
            TripReviewDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);
}
