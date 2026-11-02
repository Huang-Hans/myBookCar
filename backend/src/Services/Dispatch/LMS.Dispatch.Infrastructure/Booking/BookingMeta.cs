namespace LMS.Dispatch.Infrastructure.Booking;

// Booking 分页列表响应的 meta；Dispatch 只关心总数。
internal sealed record BookingMeta(BookingPagination? Pagination);
