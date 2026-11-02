using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Application.Configuration.Ports;
using LMS.Platform.Infrastructure.Configuration;
using LMS.Platform.Infrastructure.Configuration.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace LMS.Platform.Infrastructure.Repositories;

// The active directory is the single source; successful edits publish a new immutable snapshot.
internal sealed class MongoPropertyRepository(
    ConfigurationSnapshotStore snapshots,
    IConfigurationListSource<ConfiguredProperty> source) : IPropertyRepository
{
    public async Task<ConfiguredProperty?> FindAsync(string propertyId, CancellationToken cancellationToken)
    {
        var lookup = await source.LoadAsync(cancellationToken);
        return lookup.Value?.FirstOrDefault(property => property.PropertyId == propertyId && !property.IsDeleted);
    }

    public async Task<ConfiguredProperty?> FindAsync(string propertyId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var snapshot = await snapshots.LoadAsync(context, cancellationToken);
        var item = FindItem(snapshot, propertyId);
        if (item is null || item["isDeleted"].ToBoolean()) return null;
        return BsonSerializer.Deserialize<ConfiguredPropertyDocument>(item.AsBsonDocument).ToItem();
    }

    public async Task InsertAsync(ConfiguredProperty property, string actor, ITransactionContext context, CancellationToken cancellationToken)
    {
        var snapshot = await snapshots.LoadAsync(context, cancellationToken);
        if (FindItem(snapshot, property.PropertyId) is not null)
            throw new DuplicateRecordException("property", new InvalidOperationException("Property code already exists."));
        snapshot.PropertyItems.Add(ConfiguredPropertyDocument.FromItem(property).ToBsonDocument());
        if (!await snapshots.PublishAsync(snapshot, actor, context, cancellationToken))
            throw new DuplicateRecordException("property", new InvalidOperationException("The active configuration has changed."));
    }

    public async Task<bool> ReplaceAsync(ConfiguredProperty property, string actor, ITransactionContext context, CancellationToken cancellationToken)
    {
        var snapshot = await snapshots.LoadAsync(context, cancellationToken);
        var current = FindItem(snapshot, property.PropertyId);
        if (current is null) return false;
        var index = snapshot.PropertyItems.IndexOf(current);
        snapshot.PropertyItems[index] = ConfiguredPropertyDocument.FromItem(property).ToBsonDocument();
        return await snapshots.PublishAsync(snapshot, actor, context, cancellationToken);
    }

    public async Task<bool> DeleteAsync(ConfiguredProperty property, string actor, ITransactionContext context, CancellationToken cancellationToken)
    {
        var snapshot = await snapshots.LoadAsync(context, cancellationToken);
        var current = FindItem(snapshot, property.PropertyId);
        if (current is null || current["isDeleted"].ToBoolean()) return false;
        var index = snapshot.PropertyItems.IndexOf(current);
        snapshot.PropertyItems[index] = ConfiguredPropertyDocument.FromItem(property).ToBsonDocument();
        return await snapshots.PublishAsync(snapshot, actor, context, cancellationToken);
    }

    private static BsonValue? FindItem(ConfigurationSnapshot snapshot, string propertyId) =>
        snapshot.PropertyItems.FirstOrDefault(value => value["propertyId"].AsString == propertyId);
}
