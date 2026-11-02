using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Customers;

namespace LMS.Booking.Application.Customers.Ports;

public interface ICustomerLocationFavoritesRepository
{
    Task<CustomerLocationFavorites?> FindAsync(string guestId, FavoriteUsage usage, CancellationToken cancellationToken);

    Task<CustomerLocationFavorites?> FindAsync(string guestId, FavoriteUsage usage, ITransactionContext context, CancellationToken cancellationToken);

    // 同一客户同一用途已有记录（并发首次写入）时抛出 DuplicateRecordException。
    Task InsertAsync(CustomerLocationFavorites favorites, ITransactionContext context, CancellationToken cancellationToken);

    // 以期望版本为条件替换；版本已变化或记录不存在时返回 false。
    Task<bool> TryReplaceAsync(CustomerLocationFavorites updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);
}
