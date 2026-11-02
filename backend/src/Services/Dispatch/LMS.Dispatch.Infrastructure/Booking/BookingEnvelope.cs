namespace LMS.Dispatch.Infrastructure.Booking;

// Booking 统一响应外壳中 Dispatch 关心的部分。Meta 只在分页列表中出现。
internal sealed record BookingEnvelope<T>(int Code, T? Data, BookingMeta? Meta = null);
