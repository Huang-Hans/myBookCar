using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Application.Configuration.Options;
using LMS.Platform.Application.ConfigurationReleases.Ports;
using LMS.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Configuration;

// 读取当前生效发布下某个区域的配置正文。没有指定环境、环境没有生效发布、发布中没有该区域正文，
// 或正文无法按强类型读取时，都明确返回“不可用”，不使用任何默认或演示数据。
internal sealed class ConfigurationAssetReader(
    IMongoDatabase database,
    IActiveConfigurationRepository activeConfigurations,
    IOptions<ConfigurationAssetsOptions> options,
    ILogger<ConfigurationAssetReader> logger)
{
    public async Task<AssetLookup<TPayload>> ReadAsync<TPayload>(string areaCode, CancellationToken cancellationToken)
        where TPayload : class
    {
        var environment = options.Value.Environment;
        if (string.IsNullOrWhiteSpace(environment))
        {
            return AssetLookup<TPayload>.Unavailable;
        }

        var active = await activeConfigurations.FindAsync(environment, cancellationToken);

        return active is null
            ? AssetLookup<TPayload>.Unavailable
            : await ReadPayloadAsync<TPayload>(active.ReleaseId, areaCode, cancellationToken);
    }

    private async Task<AssetLookup<TPayload>> ReadPayloadAsync<TPayload>(string releaseId, string areaCode, CancellationToken cancellationToken)
        where TPayload : class
    {
        var id = ConfigurationAssetDocument<TPayload>.ComposeId(releaseId, areaCode);
        var collection = database.GetCollection<ConfigurationAssetDocument<TPayload>>(PlatformCollections.ConfigurationAssets);
        try
        {
            var document = await collection.Find(candidate => candidate.Id == id).FirstOrDefaultAsync(cancellationToken);

            return document is null ? AssetLookup<TPayload>.Unavailable : AssetLookup<TPayload>.Available(document.Payload);
        }
        catch (Exception exception) when (exception is FormatException or BsonException)
        {
            logger.LogError(exception, "Configuration asset {AreaCode} of release {ReleaseId} cannot be read as its typed payload.", areaCode, releaseId);
            return AssetLookup<TPayload>.Unavailable;
        }
    }
}
