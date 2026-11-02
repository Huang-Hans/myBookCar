using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Auditing;

namespace LMS.Platform.Application.Auditing.Ports;

public interface IAuditRecordRepository
{
    Task InsertAsync(AuditRecord record, ITransactionContext context, CancellationToken cancellationToken);
}
