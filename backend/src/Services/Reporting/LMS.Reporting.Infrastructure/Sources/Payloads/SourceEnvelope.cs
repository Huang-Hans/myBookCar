namespace LMS.Reporting.Infrastructure.Sources.Payloads;

// 各服务统一响应外壳中报表来源关心的部分。
internal sealed record SourceEnvelope<T>(int Code, T? Data);
