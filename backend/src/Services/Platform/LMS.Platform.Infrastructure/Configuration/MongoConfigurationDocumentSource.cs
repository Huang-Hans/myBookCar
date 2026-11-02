using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Application.Configuration.Ports;
using LMS.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace LMS.Platform.Infrastructure.Configuration;

internal sealed class MongoConfigurationDocumentSource<TModel, TDocument>(
    ConfigurationAssetReader reader,
    string areaCode,
    Func<TDocument, TModel> map,
    ILogger logger) : IConfigurationDocumentSource<TModel>
    where TDocument : class
{
    public async Task<AssetLookup<TModel>> LoadAsync(CancellationToken cancellationToken)
    {
        var lookup = await reader.ReadAsync<TDocument>(areaCode, cancellationToken);
        if (!lookup.IsAvailable)
        {
            return AssetLookup<TModel>.Unavailable;
        }

        try
        {
            return AssetLookup<TModel>.Available(map(lookup.Value!));
        }
        catch (InvalidStoredDataException exception)
        {
            logger.LogError(exception, "Configuration asset {AreaCode} contains data that cannot be mapped.", areaCode);
            return AssetLookup<TModel>.Unavailable;
        }
    }
}
