namespace LMS.Booking.Infrastructure.Remote;

// 其他服务统一响应外壳中 Booking 关心的部分。Meta 只在分页列表中出现。
internal sealed record RemoteEnvelope<T>(int Code, T? Data, List<RemoteFieldError>? Errors, RemoteMeta? Meta = null);
