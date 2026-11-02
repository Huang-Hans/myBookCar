using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Customers.Ports;
using LMS.Booking.Domain.Customers;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoCustomerAccountProfileRepository(IMongoDatabase database) : ICustomerAccountProfileRepository
{
    private IMongoCollection<CustomerAccountProfileDocument> Collection =>
        database.GetCollection<CustomerAccountProfileDocument>(BookingCollections.CustomerAccountProfiles);

    public async Task<CustomerAccountProfile?> FindAsync(string guestId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.GuestId == guestId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<CustomerAccountProfile?> FindAsync(string guestId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, candidate => candidate.GuestId == guestId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task InsertAsync(CustomerAccountProfile profile, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), CustomerAccountProfileDocument.FromDomain(profile), cancellationToken);

    public Task<bool> TryReplaceAsync(
        CustomerAccountProfile updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.GuestId,
            expectedVersion,
            CustomerAccountProfileDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);
}
