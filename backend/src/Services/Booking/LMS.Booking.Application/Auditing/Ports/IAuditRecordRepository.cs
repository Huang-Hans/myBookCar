using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Auditing;

namespace LMS.Booking.Application.Auditing.Ports;

public interface IAuditRecordRepository
{
    Task InsertAsync(AuditRecord record, ITransactionContext context, CancellationToken cancellationToken);
}
