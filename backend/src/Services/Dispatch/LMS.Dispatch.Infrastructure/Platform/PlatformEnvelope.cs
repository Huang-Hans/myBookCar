namespace LMS.Dispatch.Infrastructure.Platform;

// Platform 统一响应外壳中 Dispatch 关心的部分。
internal sealed record PlatformEnvelope<T>(int Code, T? Data);
