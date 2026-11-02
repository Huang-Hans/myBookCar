namespace LMS.ApiGateway.Documentation.Merging;

// 一次聚合的结果。IsComplete 为 false 表示有下游服务当时不可用，这份结果只缓存较短时间。
internal sealed record BuiltDocument(string Json, DateTimeOffset BuiltAt, bool IsComplete);
