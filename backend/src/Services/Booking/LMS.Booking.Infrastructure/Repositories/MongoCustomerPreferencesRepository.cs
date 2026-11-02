using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Customers.Ports;
using LMS.Booking.Domain.Customers;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoCustomerPreferencesRepository(IMongoDatabase database) : ICustomerPreferencesRepository
{
    private IMongoCollection<CustomerPreferencesDocument> Collection =>
        database.GetCollection<CustomerPreferencesDocument>(BookingCollections.CustomerPreferences);

    public async Task<CustomerPreferences?> FindAsync(string guestId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.GuestId == guestId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<CustomerPreferences?> FindAsync(string guestId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, candidate => candidate.GuestId == guestId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task InsertAsync(CustomerPreferences preferences, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), CustomerPreferencesDocument.FromDomain(preferences), cancellationToken);

    public Task<bool> TryReplaceAsync(
        CustomerPreferences updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.GuestId,
            expectedVersion,
            CustomerPreferencesDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);
}
