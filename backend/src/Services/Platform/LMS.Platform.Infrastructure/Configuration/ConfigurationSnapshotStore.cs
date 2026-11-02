using System.Security.Cryptography;
using System.Text;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Configuration.Options;
using LMS.Platform.Application.ConfigurationReleases.Ports;
using LMS.Platform.Domain.Configuration;
using LMS.Platform.Infrastructure.Persistence;
using LMS.Platform.Infrastructure.Transactions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Configuration;

// Property edits publish a complete new snapshot; historical assets remain immutable.
internal sealed class ConfigurationSnapshotStore(
    IMongoDatabase database,
    IActiveConfigurationRepository activeConfigurations,
    IConfigurationReleaseRepository releases,
    IOptions<ConfigurationAssetsOptions> options,
    TimeProvider timeProvider)
{
    private IMongoCollection<BsonDocument> Collection =>
        database.GetCollection<BsonDocument>(PlatformCollections.ConfigurationAssets);

    public async Task<ConfigurationSnapshot> LoadAsync(ITransactionContext context, CancellationToken cancellationToken)
    {
        var active = await activeConfigurations.FindAsync(options.Value.Environment!, context, cancellationToken)
            ?? throw new InvalidStoredDataException("An active configuration is required.");
        var assets = await Collection.Find(MongoTransactionContext.RequireSession(context),
            Builders<BsonDocument>.Filter.Eq("releaseId", active.ReleaseId)).ToListAsync(cancellationToken);
        return new ConfigurationSnapshot(active, assets);
    }

    public async Task<bool> PublishAsync(ConfigurationSnapshot snapshot, string actor, ITransactionContext context, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var release = ConfigurationRelease.Create(snapshot.Active.Environment, snapshot.Active.Sequence + 1,
            Digest(snapshot.Assets), actor, now, now);
        if (!await activeConfigurations.AdvanceAsync(snapshot.Active, release, context, cancellationToken)) return false;
        await releases.InsertAsync(release, context, cancellationToken);
        await InsertAssetsAsync(snapshot.Assets, release.ReleaseId, context, cancellationToken);
        return true;
    }

    private async Task InsertAssetsAsync(IReadOnlyList<BsonDocument> assets, string releaseId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        foreach (var asset in assets)
        {
            asset["_id"] = $"{releaseId}:{asset["areaCode"].AsString}";
            asset["releaseId"] = releaseId;
            await MongoInsert.OneAsync(Collection, session, asset, cancellationToken);
        }
    }

    private static string Digest(IReadOnlyList<BsonDocument> assets)
    {
        var payload = new BsonArray(assets.OrderBy(asset => asset["areaCode"].AsString, StringComparer.Ordinal)
            .Select(asset => new BsonDocument { ["areaCode"] = asset["areaCode"], ["payload"] = asset["payload"] }));
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToJson())));
    }
}
