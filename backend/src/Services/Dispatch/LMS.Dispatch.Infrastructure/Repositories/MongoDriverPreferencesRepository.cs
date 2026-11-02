using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Preferences.Ports;
using LMS.Dispatch.Domain.Preferences;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed class MongoDriverPreferencesRepository(IMongoDatabase database) : IDriverPreferencesRepository
{
    private IMongoCollection<DriverPreferencesDocument> Collection =>
        database.GetCollection<DriverPreferencesDocument>(DispatchCollections.DriverPreferences);

    public async Task<DriverPreferences?> FindAsync(string driverId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.DriverId == driverId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<DriverPreferences?> FindAsync(string driverId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, candidate => candidate.DriverId == driverId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task InsertAsync(DriverPreferences preferences, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), DriverPreferencesDocument.FromDomain(preferences), cancellationToken);

    public Task<bool> TryReplaceAsync(
        DriverPreferences updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.DriverId,
            expectedVersion,
            DriverPreferencesDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);
}
