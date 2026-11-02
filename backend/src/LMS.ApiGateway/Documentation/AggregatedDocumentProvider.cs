using System.Text.Json;
using System.Text.Json.Nodes;
using LMS.ApiGateway.Documentation.Merging;
using LMS.ApiGateway.Documentation.Sources;
using LMS.ApiGateway.Options;
using Microsoft.Extensions.Options;

namespace LMS.ApiGateway.Documentation;

// Aggregate the local gateway and available downstream documents.
// Local login documentation remains available even when no downstream document can be loaded.
internal sealed class AggregatedDocumentProvider(
    DownstreamDocumentStore store,
    GatewayDocumentSource gatewayDocument,
    TimeProvider timeProvider,
    IOptions<GatewayDocumentationOptions> options,
    ILogger<AggregatedDocumentProvider> logger)
{
    private volatile BuiltDocument? current;

    // 没有任何可用内容时返回 null。
    public async Task<string?> GetAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var cached = current;
        if (cached is not null && IsUsable(cached, now))
        {
            return cached.Json;
        }

        var built = await TryBuildAsync(now, cancellationToken);
        if (built is not null)
        {
            current = built;
            return built.Json;
        }

        return IsWithinMaxStale(cached, now) ? cached!.Json : null;
    }

    // 完整结果按缓存时间复用；缺服务的结果只复用退避时间，下游恢复后很快补齐。
    private bool IsUsable(BuiltDocument document, DateTimeOffset now)
    {
        var seconds = document.IsComplete ? options.Value.CacheSeconds : options.Value.FailureBackoffSeconds;

        return now - document.BuiltAt < TimeSpan.FromSeconds(seconds);
    }

    private bool IsWithinMaxStale(BuiltDocument? document, DateTimeOffset now) =>
        document is not null && now - document.BuiltAt < TimeSpan.FromSeconds(options.Value.MaxStaleSeconds);

    private async Task<BuiltDocument?> TryBuildAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var loaded = await Task.WhenAll(DownstreamDocuments.All.Select(source => LoadAsync(source, cancellationToken)));
        var available = loaded.Where(item => item.Document is not null).ToList();
        var unavailable = loaded.Where(item => item.Document is null).Select(item => item.Source.GroupName).ToList();

        return TryMerge(available, unavailable, now);
    }

    private async Task<(DownstreamDocument Source, CachedDocument? Document)> LoadAsync(
        DownstreamDocument source,
        CancellationToken cancellationToken) =>
        (source, await store.GetAsync(source, cancellationToken));

    // 下游文档结构异常导致合并失败时，不让文档页面报错，按“当前不可用”处理。
    private BuiltDocument? TryMerge(
        List<(DownstreamDocument Source, CachedDocument? Document)> available,
        List<string> unavailable,
        DateTimeOffset now)
    {
        try
        {
            var documents = available.Select(item => new SourceDocument(item.Source, (JsonObject)JsonNode.Parse(item.Document!.Json)!)).ToList();
            documents.Insert(0, gatewayDocument.GetDocument());

            return new BuiltDocument(OpenApiDocumentMerger.Merge(documents, unavailable), now, unavailable.Count == 0);
        }
        catch (Exception exception) when (exception is InvalidOperationException or InvalidCastException or ArgumentException or JsonException)
        {
            logger.LogError(exception, "Failed to merge the downstream API documents.");

            return null;
        }
    }
}
