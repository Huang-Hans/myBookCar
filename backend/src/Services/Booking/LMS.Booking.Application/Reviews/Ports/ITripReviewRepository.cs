using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Reviews;

namespace LMS.Booking.Application.Reviews.Ports;

public interface ITripReviewRepository
{
    Task<TripReview?> FindAsync(string tripId, string guestId, CancellationToken cancellationToken);

    Task<TripReview?> FindAsync(string tripId, string guestId, ITransactionContext context, CancellationToken cancellationToken);

    // 本人对一批行程的评价，用于给可评价行程列表附评价摘要；归属条件 guestId 写在存储查询里。
    Task<IReadOnlyList<TripReview>> FindForGuestAsync(string guestId, IReadOnlyList<string> tripIds, CancellationToken cancellationToken);

    // 同一行程已有评价（并发首次写入）时抛出 DuplicateRecordException。
    Task InsertAsync(TripReview review, ITransactionContext context, CancellationToken cancellationToken);

    // 以期望版本为条件替换；版本已变化或记录不存在时返回 false。
    Task<bool> TryReplaceAsync(TripReview updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);
}
