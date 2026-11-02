using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.ConfigurationReleases.Ports;
using LMS.Platform.Domain.Configuration;
using LMS.Platform.Infrastructure.Documents;
using LMS.Platform.Infrastructure.Persistence;
using LMS.Platform.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

internal sealed class MongoActiveConfigurationRepository(IMongoDatabase database) : IActiveConfigurationRepository
{
    private IMongoCollection<ActiveConfigurationDocument> Collection =>
        database.GetCollection<ActiveConfigurationDocument>(PlatformCollections.ActiveConfigurations);

    public async Task<ActiveConfiguration?> FindAsync(string environment, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, candidate => candidate.Environment == environment).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<ActiveConfiguration?> FindAsync(string environment, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.Environment == environment).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<bool> AdvanceAsync(
        ActiveConfiguration? current,
        ConfigurationRelease release,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        if (current is null)
        {
            await MongoInsert.OneAsync(Collection, session, ActiveConfigurationDocument.FromDomain(ActiveConfiguration.Start(release)), cancellationToken);
            return true;
        }

        return await VersionedUpdate.TryApplyAsync(
            Collection,
            session,
            Builders<ActiveConfigurationDocument>.Filter.Eq(candidate => candidate.Environment, current.Environment),
            current.Version,
            CreateSwitchUpdate(release),
            cancellationToken);
    }

    private static UpdateDefinition<ActiveConfigurationDocument> CreateSwitchUpdate(ConfigurationRelease release) =>
        Builders<ActiveConfigurationDocument>.Update
            .Set(candidate => candidate.ReleaseId, release.ReleaseId)
            .Set(candidate => candidate.Sequence, release.Sequence)
            .Set(candidate => candidate.UpdatedAt, UtcDateTimes.ToStored(release.CreatedAt));
}
