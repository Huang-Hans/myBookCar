using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Customers;

namespace LMS.Booking.Application.Customers.Ports;

public interface ICustomerAccountProfileRepository
{
    Task<CustomerAccountProfile?> FindAsync(string guestId, CancellationToken cancellationToken);

    Task<CustomerAccountProfile?> FindAsync(string guestId, ITransactionContext context, CancellationToken cancellationToken);

    // 同一客户已有记录（并发首次写入）时抛出 DuplicateRecordException。
    Task InsertAsync(CustomerAccountProfile profile, ITransactionContext context, CancellationToken cancellationToken);

    // 以期望版本为条件替换；版本已变化或记录不存在时返回 false。
    Task<bool> TryReplaceAsync(CustomerAccountProfile updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);
}
