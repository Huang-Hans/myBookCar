using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using LMS.ApiGateway.Options;
using Microsoft.Extensions.Options;

namespace LMS.ApiGateway.Documentation.Sources;

// 带缓存与容错地读取下游文档：新鲜期内直接返回缓存；失败时退避，并继续使用上一次成功的文档。
internal sealed class DownstreamDocumentStore(
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider,
    IOptions<GatewayDocumentationOptions> options,
    ILogger<DownstreamDocumentStore> logger)
{
    private const string ClientName = "platform-api-document";

    private readonly ConcurrentDictionary<string, DocumentState> states = new();

    // 下游不可用且没有可用的缓存时返回 null。
    public async Task<CachedDocument?> GetAsync(DownstreamDocument source, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var state = states.GetValueOrDefault(source.Key, DocumentState.Empty);
        var settings = options.Value;
        if (state.CanServeWithoutFetch(now, TimeSpan.FromSeconds(settings.CacheSeconds), TimeSpan.FromSeconds(settings.FailureBackoffSeconds)))
        {
            return state.Document;
        }

        var next = await RefreshAsync(source, state, now, cancellationToken);
        states[source.Key] = next;

        return next.Document;
    }

    private async Task<DocumentState> RefreshAsync(
        DownstreamDocument source,
        DocumentState current,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var json = await TryFetchAsync(source, cancellationToken);

        return json is null
            ? current.Failed(now, TimeSpan.FromSeconds(options.Value.MaxStaleSeconds))
            : DocumentState.Loaded(new CachedDocument(json, now));
    }

    private async Task<string?> TryFetchAsync(DownstreamDocument source, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.FetchTimeoutSeconds));
        try
        {
            using var client = httpClientFactory.CreateClient(ClientName);
            var json = await client.GetStringAsync(source.Url, timeout.Token);

            return JsonNode.Parse(json) is JsonObject ? json : null;
        }
        catch (Exception exception) when (IsDownstreamFailure(exception, cancellationToken))
        {
            logger.LogWarning("Failed to read the API document of {Service}: {Reason}", source.Key, exception.GetType().Name);

            return null;
        }
    }

    // 调用方主动取消不算下游故障，继续向上抛出。
    private static bool IsDownstreamFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException
        && !cancellationToken.IsCancellationRequested;
}
