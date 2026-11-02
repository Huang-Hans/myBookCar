namespace LMS.ApiGateway.Documentation.Sources;

// 单个下游文档的缓存状态：最近一次成功取得的文档，以及最近一次失败的时间。
internal sealed record DocumentState(CachedDocument? Document, DateTimeOffset? FailedAt)
{
    public static readonly DocumentState Empty = new(null, null);

    public static DocumentState Loaded(CachedDocument document) => new(document, null);

    // 文档仍新鲜，或刚失败过正处于退避期，都不需要再请求下游。
    public bool CanServeWithoutFetch(DateTimeOffset now, TimeSpan cacheDuration, TimeSpan failureBackoff) =>
        IsFresh(now, cacheDuration) || IsBackingOff(now, failureBackoff);

    // 取文档失败：保留上一次成功的文档继续使用，但超过最长陈旧时间后丢弃。
    public DocumentState Failed(DateTimeOffset now, TimeSpan maxStale)
    {
        var stillUsable = Document is not null && now - Document.FetchedAt < maxStale;

        return new DocumentState(stillUsable ? Document : null, now);
    }

    private bool IsFresh(DateTimeOffset now, TimeSpan cacheDuration) =>
        Document is not null && now - Document.FetchedAt < cacheDuration;

    private bool IsBackingOff(DateTimeOffset now, TimeSpan failureBackoff) =>
        FailedAt is not null && now - FailedAt.Value < failureBackoff;
}
