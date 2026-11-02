namespace LMS.Dispatch.Api.Contracts.Internal;

/// <summary>
/// 司机释放请求正文：司机执行的行程已结束。
/// </summary>
/// <param name="TripId">已完成或已取消的行程标识。</param>
/// <param name="EndedAt">行程结束时间，ISO 8601 UTC 并以 Z 结尾。</param>
public sealed record DriverReleaseRequest(string? TripId, DateTimeOffset? EndedAt);
