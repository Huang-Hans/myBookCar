using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Application.Configuration.Ports;
using LMS.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace LMS.Platform.Infrastructure.Configuration;

internal sealed class MongoConfigurationListSource<TItem, TDocument>(
    ConfigurationAssetReader reader,
    string areaCode,
    Func<TDocument, TItem> map,
    ILogger logger) : IConfigurationListSource<TItem>
{
    public async Task<AssetLookup<IReadOnlyList<TItem>>> LoadAsync(CancellationToken cancellationToken)
    {
        var lookup = await reader.ReadAsync<ItemsPayloadDocument<TDocument>>(areaCode, cancellationToken);
        if (!lookup.IsAvailable)
        {
            return AssetLookup<IReadOnlyList<TItem>>.Unavailable;
        }

        try
        {
            IReadOnlyList<TItem> items = lookup.Value!.Items.Select(map).ToList();
            return AssetLookup<IReadOnlyList<TItem>>.Available(items);
        }
        catch (InvalidStoredDataException exception)
        {
            logger.LogError(exception, "Configuration asset {AreaCode} contains data that cannot be mapped.", areaCode);
            return AssetLookup<IReadOnlyList<TItem>>.Unavailable;
        }
    }
}
