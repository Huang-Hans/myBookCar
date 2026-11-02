using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Auditing;

namespace LMS.Dispatch.Application.Auditing.Ports;

public interface IAuditRecordRepository
{
    Task InsertManyAsync(IReadOnlyList<AuditRecord> records, ITransactionContext context, CancellationToken cancellationToken);

    Task InsertAsync(AuditRecord record, ITransactionContext context, CancellationToken cancellationToken);
}
