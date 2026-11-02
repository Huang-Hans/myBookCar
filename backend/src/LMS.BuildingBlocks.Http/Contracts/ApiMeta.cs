namespace LMS.BuildingBlocks.Http.Contracts;

/// <summary>
/// 响应元数据。
/// </summary>
/// <param name="TraceId">W3C trace id，与顶层 traceId 完全一致。</param>
/// <param name="Pagination">分页列表必带；非分页响应省略。</param>
public sealed record ApiMeta(string TraceId, ApiPagination? Pagination = null);
