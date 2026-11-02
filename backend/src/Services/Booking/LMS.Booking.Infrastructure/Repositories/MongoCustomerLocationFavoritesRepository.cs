using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Customers.Ports;
using LMS.Booking.Domain.Customers;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoCustomerLocationFavoritesRepository(IMongoDatabase database) : ICustomerLocationFavoritesRepository
{
    private IMongoCollection<CustomerLocationFavoritesDocument> Collection =>
        database.GetCollection<CustomerLocationFavoritesDocument>(BookingCollections.CustomerLocationFavorites);

    public async Task<CustomerLocationFavorites?> FindAsync(string guestId, FavoriteUsage usage, CancellationToken cancellationToken)
    {
        var id = CustomerLocationFavoritesDocument.IdOf(guestId, usage);
        var document = await Collection.Find(candidate => candidate.Id == id).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<CustomerLocationFavorites?> FindAsync(
        string guestId,
        FavoriteUsage usage,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var id = CustomerLocationFavoritesDocument.IdOf(guestId, usage);
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, candidate => candidate.Id == id).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task InsertAsync(CustomerLocationFavorites favorites, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), CustomerLocationFavoritesDocument.FromDomain(favorites), cancellationToken);

    public Task<bool> TryReplaceAsync(
        CustomerLocationFavorites updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            CustomerLocationFavoritesDocument.IdOf(updated.GuestId, updated.Usage),
            expectedVersion,
            CustomerLocationFavoritesDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);
}
