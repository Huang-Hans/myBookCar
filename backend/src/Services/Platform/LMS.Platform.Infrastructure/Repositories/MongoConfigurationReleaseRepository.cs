using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.ConfigurationReleases.Ports;
using LMS.Platform.Domain.Configuration;
using LMS.Platform.Infrastructure.Documents;
using LMS.Platform.Infrastructure.Persistence;
using LMS.Platform.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

internal sealed class MongoConfigurationReleaseRepository(IMongoDatabase database) : IConfigurationReleaseRepository
{
    private IMongoCollection<ConfigurationReleaseDocument> Collection =>
        database.GetCollection<ConfigurationReleaseDocument>(PlatformCollections.ConfigurationReleases);

    public async Task<ConfigurationRelease?> FindAsync(string releaseId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.ReleaseId == releaseId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task InsertAsync(ConfigurationRelease release, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            ConfigurationReleaseDocument.FromDomain(release),
            cancellationToken);
}
